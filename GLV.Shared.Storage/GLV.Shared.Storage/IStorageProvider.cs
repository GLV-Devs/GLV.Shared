using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Storage;

public enum EntryType : byte
{
    File,
    Directory,
    Link
}

public readonly record struct EntryInfo(string Path, EntryType Type);

public interface IBasicStorageProvider : IDisposable, IAsyncDisposable
{
    public string Provider { get; }
    public string? Root { get; }

    public void WriteData(string path, FileMode mode, ReadOnlySpan<byte> data);
    public void WriteData(string path, FileMode mode, IEnumerable<byte> data);
    public void WriteData(string path, FileMode mode, Stream data);
    public Task WriteDataAsync(string path, FileMode mode, IEnumerable<byte> data, CancellationToken ct = default);
    public Task WriteDataAsync(string path, FileMode mode, Stream data, CancellationToken ct = default);
    public Task WriteDataAsync(string path, FileMode mode, byte[] data, CancellationToken ct = default);
    public Stream GetReadStream(string path);
    public ValueTask<Stream> GetReadStreamAsync(string path, CancellationToken ct = default);
    public Stream GetWriteStream(string path, FileMode mode);
    public ValueTask<Stream> GetWriteStreamAsync(string path, FileMode mode, CancellationToken ct = default);

    public bool FileExists(string path);
    public Task<bool> FileExistsAsync(string path, CancellationToken ct = default);

    public bool DeleteFile(string path);
    public Task<bool> DeleteFileAsync(string path, CancellationToken ct = default);

    public IEnumerable<string> ListFiles(string? path = null);
}

public interface IStorageProvider : IBasicStorageProvider, IDisposable, IAsyncDisposable
{

    [return: NotNullIfNotNull(nameof(path))]
    public string? PreparePath(string? path);

    public bool TryGetAbsolutePath(string? path, [NotNullWhen(true)] out string? result);

    public bool DeleteDirectory(string path, bool recursive = false);
    public Task<bool> DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken ct = default);

    public bool CreateDirectory(string path);
    public Task<bool> CreateDirectoryAsync(string path, CancellationToken ct = default);

    public bool DirectoryExists(string path);
    public Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default);

    public void MoveFile(string path, string newPath, bool overwrite = false);
    public Task MoveFileAsync(string path, string newPath, bool overwrite = false, CancellationToken ct = default);

    public void CopyFile(string path, string newPath, bool overwrite = false);
    public Task CopyFileAsync(string path, string newPath, bool overwrite = false, CancellationToken ct = default);

    public async IAsyncEnumerable<string> ListFilesAsync(string? path = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var file in ListEntriesAsync(path, ct))
            if (file.Type is EntryType.File)
                yield return file.Path;
    }

    public IEnumerable<string> ListDirectories(string? path = null)
    {
        foreach (var file in ListEntries(path))
            if (file.Type is EntryType.Directory)
                yield return file.Path;
    }

    public async IAsyncEnumerable<string> ListDirectoriesAsync(string? path = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var file in ListEntriesAsync(path, ct))
            if (file.Type is EntryType.Directory)
                yield return file.Path;
    }

    public IEnumerable<string> ListLinks(string? path = null)
    {
        foreach (var file in ListEntries(path))
            if (file.Type is EntryType.Link)
                yield return file.Path;
    }

    public async IAsyncEnumerable<string> ListLinksAsync(string? path = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var file in ListEntriesAsync(path, ct))
            if (file.Type is EntryType.Link)
                yield return file.Path;
    }

    public IEnumerable<EntryInfo> ListEntries(string? path = null);
    public IAsyncEnumerable<EntryInfo> ListEntriesAsync(string? path = null, CancellationToken ct = default);
}
