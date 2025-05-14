using GLV.Shared.Storage.Abstractions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GLV.Shared.Storage.GoogleCloud;

public class GoogleCloudStorageProvider : IStorageProvider
{
    public const string ProviderName = "GoogleCloud.V1";

    public string Provider => ProviderName;
    public string? Root { get; }

    public void WriteData(string path, FileMode mode, ReadOnlySpan<byte> data)
    {
        throw new NotImplementedException();
    }

    public void WriteData(string path, FileMode mode, IEnumerable<byte> data)
    {
        throw new NotImplementedException();
    }

    public void WriteData(string path, FileMode mode, Stream data)
    {
        throw new NotImplementedException();
    }

    public Task WriteDataAsync(string path, FileMode mode, IEnumerable<byte> data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task WriteDataAsync(string path, FileMode mode, Stream data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task WriteDataAsync(string path, FileMode mode, byte[] data, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Stream GetReadStream(string path)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Stream> GetReadStreamAsync(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Stream GetWriteStream(string path, FileMode mode)
    {
        throw new NotImplementedException();
    }

    public ValueTask<Stream> GetWriteStreamAsync(string path, FileMode mode, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    [return: NotNullIfNotNull("path")]
    public string? PreparePath(string? path)
    {
        throw new NotImplementedException();
    }

    public bool TryGetAbsolutePath(string? path, [NotNullWhen(true)] out string? result)
    {
        throw new NotImplementedException();
    }

    public bool DeleteDirectory(string path, bool recursive = false)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public bool CreateDirectory(string path)
    {
        throw new NotImplementedException();
    }

    public Task<bool> CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public bool DirectoryExists(string path)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void MoveFile(string path, string newPath, bool overwrite = false)
    {
        throw new NotImplementedException();
    }

    public Task MoveFileAsync(string path, string newPath, bool overwrite = false, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void CopyFile(string path, string newPath, bool overwrite = false)
    {
        throw new NotImplementedException();
    }

    public Task CopyFileAsync(string path, string newPath, bool overwrite = false, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public bool DeleteFile(string path)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteFileAsync(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public bool FileExists(string path)
    {
        throw new NotImplementedException();
    }

    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<EntryInfo> ListEntries(string? path = null)
    {
        throw new NotImplementedException();
    }

    public IAsyncEnumerable<EntryInfo> ListEntriesAsync(string? path = null, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }

    public ValueTask DisposeAsync()
    {
        throw new NotImplementedException();
    }
}
