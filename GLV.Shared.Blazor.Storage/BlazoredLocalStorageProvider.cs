using GLV.Shared.Common;
using GLV.Shared.Storage;
using Microsoft.JSInterop;
using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Threading;

namespace GLV.Shared.Blazor.Storage;

internal sealed class BrowserWriteStream : MemoryStream
{
    private readonly string key;
    private readonly IJSRuntime runtime;

    public BrowserWriteStream(string key, IJSRuntime runtime)
    {
        this.key = key;
        this.runtime = runtime;
        StreamRef = new(this);
    }

    public DotNetStreamReference StreamRef { get; }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        runtime.InvokeVoidAsync("setLocalStorageValue", key, StreamRef).ConfigureAwait(false).GetAwaiter().GetResult();
        StreamRef.Dispose();
    }
}

public class BrowserLocalStorageProvider(
    IJSRuntime jsRuntime,
    string? root = null
) : IBasicStorageProvider
{
    private readonly IJSRuntime JSRuntime = jsRuntime;
    private readonly IJSInProcessRuntime? JSInProcessRuntime = jsRuntime as IJSInProcessRuntime;
    
    public string Provider => "Browser Local Storage";
    public string? Root { get; } = root;

    [return: NotNullIfNotNull(nameof(path))]
    private string? PrependRoot(string? path)
        => string.IsNullOrWhiteSpace(Root) ? path : $"{Root}:-:{path}";

    public void WriteData(string path, FileMode mode, ReadOnlySpan<byte> data)
    {
        SetItem(PrependRoot(path), Convert.ToBase64String(data));
    }

    public void WriteData(string path, FileMode mode, IEnumerable<byte> data)
    {
        var count = data.Count();
        var span = ArrayPoolHelper.TryRent<byte>(count, out var rental) ? rental.Span : stackalloc byte[count];
        using (rental)
            SetItem(PrependRoot(path), Convert.ToBase64String(rental.Span));
    }

    public void WriteData(string path, FileMode mode, Stream data)
    {
        using var streamRef = new DotNetStreamReference(data, true);
        JSRuntime.InvokeVoidAsync("setLocalStorageValue", PrependRoot(path), streamRef).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    public Task WriteDataAsync(string path, FileMode mode, IEnumerable<byte> data, CancellationToken ct = default)
    {
        var count = data.Count();
        var span = ArrayPoolHelper.TryRent<byte>(count, out var rental) ? rental.Span : stackalloc byte[count];
        using (rental)
            return SetItemAsync(PrependRoot(path), Convert.ToBase64String(rental.Span)).AsTask();
    }

    public Task WriteDataAsync(string path, FileMode mode, Stream data, CancellationToken ct = default)
    {
        using var streamRef = new DotNetStreamReference(data, true);
        return JSRuntime.InvokeVoidAsync("setLocalStorageValue", ct, PrependRoot(path), streamRef).AsTask();
    }

    public Task WriteDataAsync(string path, FileMode mode, byte[] data, CancellationToken ct = default)
    {
        return SetItemAsync(PrependRoot(path), Convert.ToBase64String(data)).AsTask();
    }

    public Stream GetReadStream(string path)
        => OpenReadStream(PrependRoot(path));

    public ValueTask<Stream> GetReadStreamAsync(string path, CancellationToken ct = default)
        => OpenReadStreamAsync(PrependRoot(path), ct);

    public Stream GetWriteStream(string path, FileMode mode)
        => OpenWriteStream(PrependRoot(path));

    public ValueTask<Stream> GetWriteStreamAsync(string path, FileMode mode, CancellationToken ct = default)
        => ValueTask.FromResult(OpenWriteStream(PrependRoot(path)));

    public bool FileExists(string path)
        => ContainKey(PrependRoot(path));

    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
        => ContainKeyAsync(PrependRoot(path), ct).AsTask();

    public bool DeleteFile(string path)
    {
        RemoveItem(PrependRoot(path));
        return true;
    }

    public async Task<bool> DeleteFileAsync(string path, CancellationToken ct = default)
    {
        await RemoveItemAsync(PrependRoot(path), ct);
        return true;
    }

    public IEnumerable<string> ListFiles(string? path = null)
    {
        var p = PrependRoot(path);
        return string.IsNullOrWhiteSpace(p) ? Keys() : Keys().Where(x => x.StartsWith(p, StringComparison.OrdinalIgnoreCase));
    }

    public void Dispose() { }

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    #region Internal

    protected static bool IsStorageDisabledException(Exception exception)
        => exception.Message.Contains("Failed to read the 'localStorage' property from 'Window'");

    private ValueTask<Stream> OpenReadStreamAsync(string key, CancellationToken ct)
    {
        if (!ContainKey(key))
            throw new FileNotFoundException();

        CheckForInProcessRuntime();
        try
        {
            var streamRef = JSInProcessRuntime.Invoke<IJSStreamReference>("getLocalStorageValue", key);
            return streamRef.OpenReadStreamAsync(512000, ct);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private Stream OpenReadStream(string key)
    {
        if (!ContainKey(key))
            throw new FileNotFoundException();

        CheckForInProcessRuntime();
        try
        {
            var streamRef = JSInProcessRuntime.Invoke<IJSStreamReference>("getLocalStorageValue", key);
            return streamRef.OpenReadStreamAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private Stream OpenWriteStream(string key)
    {
        try
        {
            return new BrowserWriteStream(key, JSRuntime);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private ValueTask<string> GetItemAsync(string key)
    {
        try
        {
            return JSRuntime.InvokeAsync<string>("localStorage.getItem", key);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private string GetItem(string key)
    {
        CheckForInProcessRuntime();
        try
        {
            return JSInProcessRuntime.Invoke<string>("localStorage.getItem", key);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private ValueTask SetItemAsync(string key, string data)
    {
        try
        {
            return JSRuntime.InvokeVoidAsync("localStorage.setItem", key, data);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private void SetItem(string key, string data)
    {
        CheckForInProcessRuntime();
        try
        {
            JSInProcessRuntime.InvokeVoid("localStorage.setItem", key, data);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private void Clear()
    {
        CheckForInProcessRuntime();
        try
        {
            JSInProcessRuntime.InvokeVoid("localStorage.clear");
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private async ValueTask ClearAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.clear", cancellationToken);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private bool ContainKey(string key)
    {
        CheckForInProcessRuntime();
        try
        {
            return JSInProcessRuntime.Invoke<bool>("localStorage.hasOwnProperty", key);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private async ValueTask<bool> ContainKeyAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await JSRuntime.InvokeAsync<bool>("localStorage.hasOwnProperty", cancellationToken, key);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private IEnumerable<string> Keys()
    {
        CheckForInProcessRuntime();
        try
        {
            return JSInProcessRuntime.Invoke<IEnumerable<string>>("eval", "Object.keys(localStorage)");
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private async ValueTask<IEnumerable<string>> KeysAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await JSRuntime.InvokeAsync<IEnumerable<string>>("eval", cancellationToken, "Object.keys(localStorage)");
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private void RemoveItem(string key)
    {
        CheckForInProcessRuntime();
        try
        {
            JSInProcessRuntime.InvokeVoid("localStorage.removeItem", key);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    private async ValueTask RemoveItemAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await JSRuntime.InvokeVoidAsync("localStorage.removeItem", cancellationToken, key);
        }
        catch (Exception exception)
        {
            if (IsStorageDisabledException(exception))
            {
                throw new IOException("Browser Local Storage is disabled", exception);
            }

            throw;
        }
    }

    [MemberNotNull(nameof(JSInProcessRuntime))]
    protected void CheckForInProcessRuntime()
    {
        if (JSInProcessRuntime == null)
            throw new InvalidOperationException("IJSInProcessRuntime not available");
    }

    #endregion
}
