﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CG.Web.MegaApiClient;
using GLV.Shared.Storage.Abstractions;
using NeoSmart.AsyncLock;

namespace GLV.Shared.Storage.MEGA;

public class MEGAStorageProvider(MegaApiClient.AuthInfos? authInfo, IMegaApiClient? client = null, string? root = null, bool fallbackToAnonymousLogin = true, bool releaseClientOnDispose = true, TimeSpan? nodeRefreshTime = null) : IStorageProvider
{
    public const string ProviderName = "MEGA";

    public string Provider => ProviderName;
    public string? Root { get; } = root;

    public IMegaApiClient Client { get; } = client ?? new MegaApiClient();
    public bool LogoutClientOnDispose { get; } = releaseClientOnDispose;

    private bool HasRoot => string.IsNullOrWhiteSpace(Root) is false;

    private readonly AsyncLock AsyncLock = new();
    private readonly Stopwatch LastRefresh = new();
    private readonly TimeSpan NodeRefreshTime = nodeRefreshTime ?? TimeSpan.FromMinutes(10);
    private readonly MegaApiClient.AuthInfos? AuthInfo = authInfo;

    private IEnumerable<INode>? nodeCache;
    private INode? root;
    private MegaApiClient.LogonSessionToken? Token;

    public MEGAStorageProvider(string email, string password, string? mfaKey = null, IMegaApiClient? client = null, string? root = null, bool fallbackToAnonymousLogin = true, bool releaseClientOnDispose = true, TimeSpan? nodeRefreshTime = null)
        : this(null, client, root, fallbackToAnonymousLogin, releaseClientOnDispose, nodeRefreshTime)
    {
        AuthInfo = Client.GenerateAuthInfos(email, password, mfaKey);
    }

    #region Synchronous Login

    private static bool TryRestoreSession(IMegaApiClient client, MegaApiClient.LogonSessionToken token, [NotNullWhen(false)] out Exception? excp)
    {
        ArgumentNullException.ThrowIfNull(token);
        try
        {
            client.Login(token);
            excp = null;
            return true;
        }
        catch(ApiException e)
        {
            excp = e;
            return false;
        }
    }

    private bool TryLogin(MegaApiClient.AuthInfos authInfo, [NotNullWhen(false)] out Exception? excp)
    {
        ArgumentNullException.ThrowIfNull(authInfo);
        try
        {
            Token = Client.Login(authInfo);
            excp = null;
            return true;
        }
        catch(ApiException e)
        {
            excp = e;
            return false;
        }
    }

    private static bool TryLoginAnonymous(IMegaApiClient client)
    {
        client.LoginAnonymous();
        return true;
    }

    protected void EnsureLoggedIn()
    {
        Exception? excp = null;
        if (Client.IsLoggedIn is false)
            if ((Token is not null && TryRestoreSession(Client, Token, out excp)
                || AuthInfo is not null && TryLogin(AuthInfo, out excp))
            is false)
            {
                if (fallbackToAnonymousLogin is true && TryLoginAnonymous(Client))
                    return;
                else if (excp is not null)
                    throw excp;
                else
                    throw new InvalidOperationException("Could not log in into MEGA");
            }
    }

    #endregion

    public INode GetRootNode()
    {
        EnsureLoggedIn();
        if (root is null || LastRefresh.Elapsed > NodeRefreshTime)
            using (AsyncLock.Lock())
                root = InternalGetNode(Root, Client.GetNodes());

        Debug.Assert(root is not null);
        return root;
    }

    public async ValueTask<INode> GetRootNodeAsync()
    {
        EnsureLoggedIn();
        if (root is null || LastRefresh.Elapsed > NodeRefreshTime)
            using (await AsyncLock.LockAsync())
            {
                root = InternalGetNode(Root, await Client.GetNodesAsync());
                if (root is null)
                {
                    var megaRoot = (await Client.GetNodesAsync()).First(x => x.Type == NodeType.Root);
                    var parent = megaRoot;

                    if (string.IsNullOrWhiteSpace(Root))
                        root = megaRoot;
                    else
                    {
                        foreach (var part in PreparePath(Root).Split('/'))
                            parent = await Client.CreateFolderAsync(part, parent);
                        root = parent;
                    }
                }
            }

        Debug.Assert(root is not null);
        return root;
    }

    internal void InvalidateCache()
    {
        nodeCache = null;
    }

    private IEnumerable<INode> GetFromCacheOrRequestNodes()
    {
        if (nodeCache is null || LastRefresh.Elapsed > NodeRefreshTime)
            using (AsyncLock.Lock())
            {
                if (HasRoot)
                {
                    var root = GetRootNode();
                    var list = Client.GetNodes(root).Where(n => n.Type is not NodeType.Inbox and not NodeType.Trash).ToList();
                    list.Add(root);
                    nodeCache = list;
                }
                else
                {
                    var nodes = Client.GetNodes();
                    var trash = nodes.First(x => x.Type == NodeType.Trash);
                    var inbox = nodes.First(x => x.Type == NodeType.Inbox);

                    nodeCache = nodes.Where(n => n.Type is not NodeType.Inbox and not NodeType.Trash && n.ParentId != trash.Id && n.ParentId != inbox.Id).ToList();
                }
            }

        return nodeCache;
    }

    private async ValueTask<IEnumerable<INode>> GetFromCacheOrRequestNodesAsync()
    {
        if (nodeCache is null || LastRefresh.Elapsed > NodeRefreshTime)
            using (await AsyncLock.LockAsync())
            {
                if (HasRoot)
                {
                    var root = await GetRootNodeAsync();
                    var list = (await Client.GetNodesAsync(root)).Where(n => n.Type is not NodeType.Inbox and not NodeType.Trash).ToList();
                    list.Add(root);
                    nodeCache = list;
                }
                else
                    nodeCache = (await Client.GetNodesAsync()).Where(n => n.Type is not NodeType.Inbox and not NodeType.Trash).ToList();
            }

        return nodeCache;
    }

    [return: NotNullIfNotNull(nameof(path))]
    public string? PreparePath(string? path)
        => path?.Replace('\\', '/');

    public async Task<INode?> GetNodeAsync(string? path)
        => InternalGetNode(path, await GetFromCacheOrRequestNodesAsync());

    public INode? GetNode(string? path)
        => InternalGetNode(path, GetFromCacheOrRequestNodes());

    private INode? InternalGetNode(string? path, IEnumerable<INode> nodes)
    {
        EnsureLoggedIn();
        var cnode = nodes.FirstOrDefault(x => x.Type == NodeType.Root || x.Id == GetRootNode().Id);

        if (string.IsNullOrWhiteSpace(path))
            return cnode;

        var nodePath = PreparePath(path).Split('/');

        if (nodePath is null or { Length: 0 })
            return null;

        for (int level = 0; level < nodePath.Length; level++)
        {
            if (cnode is null)
                return null;
            else
                cnode = nodes.FirstOrDefault(x => x.ParentId == cnode.Id && x.Name == nodePath[level]);
        }

        return cnode;
    }

    public void WriteData(string path, FileMode mode, ReadOnlySpan<byte> data)
    {
        using var dat = new MemoryStream(data.Length);
        dat.Write(data);
        dat.Position = 0;
        InvalidateCache();
        WriteData(path, mode, dat);
    }

    public void WriteData(string path, FileMode mode, IEnumerable<byte> data)
    {
        using var dat = new MemoryStream();
        foreach (var b in data)
            dat.WriteByte(b);
        dat.Position = 0;
        InvalidateCache();
        WriteData(path, mode, dat);
    }

    public void WriteData(string path, FileMode mode, Stream data)
    {
        if (mode is FileMode.Open or FileMode.OpenOrCreate)
            throw new ArgumentException($"FileMode.Open and FileMode.OpenOrCreate are not valid for writing");

        var original = GetNode(path);
        if (mode is FileMode.CreateNew or 0)
        {
            if (original is not null)
                throw new IOException($"Cannot create file \"{path}\" because there is already an entry with that same name");
        }
        else if (mode is FileMode.Create)
        {
            if (original is not null)
                Client.Delete(original);
        }
        else if (mode is FileMode.Truncate)
        {
            if (original is null)
                throw new FileNotFoundException($"Could not find file {path} to truncate");
        }
        else if (mode is FileMode.Append)
        {
            throw new NotSupportedException("FileMode.Append is not currently supported");
        }
        else
            throw new ArgumentException($"Unsupported FileMode: {mode}");

        var parentname = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(path).Value;
        var parent = GetNode(parentname) ?? throw new DirectoryNotFoundException($"Could not find parent directory \"{parentname}\"");

        if (parent.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot create a file under a MEGA node of a type that is not Directory or Root -- The type of the node \"{parentname}\" was {parent.Type}");

        var filename = Regexes.GetFileOrDirectoryNameRegex().Match(path).Value;
        InvalidateCache();
        Client.Upload(data, filename, parent);
    }

    public Task WriteDataAsync(string path, FileMode mode, IEnumerable<byte> data, CancellationToken ct = default)
    {
        using var dat = new MemoryStream();
        foreach (var b in data)
            dat.WriteByte(b);
        dat.Position = 0;
        InvalidateCache();
        return WriteDataAsync(path, mode, dat, ct);
    }

    public async Task WriteDataAsync(string path, FileMode mode, byte[] data, CancellationToken ct = default)
    {
        using var dat = new MemoryStream();
        await dat.WriteAsync(data, ct);
        dat.Position = 0;
        InvalidateCache();
        await WriteDataAsync(path, mode, dat, ct);
    }

    public async Task WriteDataAsync(string path, FileMode mode, Stream data, CancellationToken ct = default)
    {
        if (mode is FileMode.Open or FileMode.OpenOrCreate)
            throw new ArgumentException($"FileMode.Open and FileMode.OpenOrCreate are not valid for writing");

        var original = await GetNodeAsync(path);
        if (mode is FileMode.CreateNew or 0)
        {
            if (original is not null)
                throw new IOException($"Cannot create file \"{path}\" because there is already an entry with that same name");
        }
        else if (mode is FileMode.Create)
        {
            if (original is not null)
                await Client.DeleteAsync(original);
        }
        else if (mode is FileMode.Truncate)
        {
            if (original is null)
                throw new FileNotFoundException($"Could not find file {path} to truncate");
        }
        else if (mode is FileMode.Append)
        {
            throw new NotSupportedException("FileMode.Append is not currently supported");
        }
        else
            throw new ArgumentException($"Unknown FileMode: {mode}");

        var parentname = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(path).Value;
        var parent = GetNode(parentname) ?? throw new DirectoryNotFoundException($"Could not find parent directory \"{parentname}\"");

        if (parent.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot create a file under a MEGA node of a type that is not Directory or Root -- The type of the node \"{parentname}\" was {parent.Type}");

        var filename = Regexes.GetFileOrDirectoryNameRegex().Match(path).Value;
        InvalidateCache();
        await Client.UploadAsync(data, filename, parent, cancellationToken: ct);
    }

    public Stream GetReadStream(string path)
        => Client.Download(GetNode(path) ?? throw new FileNotFoundException($"Could not find file \"{path}\""));

    public async ValueTask<Stream> GetReadStreamAsync(string path, CancellationToken ct = default)
        => await Client.DownloadAsync(GetNode(path) ?? throw new FileNotFoundException($"Could not find file \"{path}\""), null, ct);

    public Stream GetWriteStream(string path, FileMode mode)
        => new MEGAStorageBufferedStream(this, path, mode);

    public ValueTask<Stream> GetWriteStreamAsync(string path, FileMode mode, CancellationToken ct = default)
        => ValueTask.FromResult(GetWriteStream(path, mode));

    public bool DeleteDirectory(string path, bool recursive = false)
    {
        var n = GetNode(path) ?? throw new DirectoryNotFoundException($"Could not find the Directory Node {path}");

        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new InvalidOperationException($"Cannot delete node \"{path}\", because it is not a directory; instead it's of type {n.Type}");

        if (recursive is false && GetFromCacheOrRequestNodes().Any(x => x.ParentId == n.Id))
            return false;

        InvalidateCache();
        Client.Delete(n, true);
        return true;
    }

    public async Task<bool> DeleteDirectoryAsync(string path, bool recursive = false, CancellationToken ct = default)
    {
        var n = await GetNodeAsync(path) ?? throw new DirectoryNotFoundException($"Could not find the Directory Node {path}");

        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new InvalidOperationException($"Cannot delete node \"{path}\", because it is not a directory; instead it's of type {n.Type}");

        if (recursive is false && (await GetFromCacheOrRequestNodesAsync()).Any(x => x.ParentId == n.Id))
            return false;

        InvalidateCache();
        await Client.DeleteAsync(n, true);
        return true;
    }

    public bool CreateDirectory(string path)
    {
        var n = GetNode(path);

        if (n is not null && n.Type is not NodeType.Directory and not NodeType.Root)
            throw new InvalidOperationException($"Cannot create directory node \"{path}\", because it already exists and is not a directory; instead it's of type {n.Type}");
        else if (n is not null)
            return false;

        var parentname = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(path).Value;
        var parent = GetNode(parentname) ?? throw new DirectoryNotFoundException($"Could not find parent directory \"{parentname}\"");

        if (parent.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot create a file under a MEGA node of a type that is not Directory or Root -- The type of the node \"{parentname}\" was {parent.Type}");

        var filename = Regexes.GetFileOrDirectoryNameRegex().Match(path).Value;
        InvalidateCache();
        Client.CreateFolder(filename, parent);
        return true;
    }

    public async Task<bool> CreateDirectoryAsync(string path, CancellationToken ct = default)
    {
        var n = await GetNodeAsync(path);

        if (n is not null && n.Type is not NodeType.Directory and not NodeType.Root)
            throw new InvalidOperationException($"Cannot create directory node \"{path}\", because it already exists and is not a directory; instead it's of type {n.Type}");
        else if (n is not null)
            return false;

        var parentname = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(path).Value;
        var parent = await GetNodeAsync(parentname) ?? throw new DirectoryNotFoundException($"Could not find parent directory \"{parentname}\"");

        if (parent.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot create a file under a MEGA node of a type that is not Directory or Root -- The type of the node \"{parentname}\" was {parent.Type}");

        var filename = Regexes.GetFileOrDirectoryNameRegex().Match(path).Value;
        InvalidateCache();
        await Client.CreateFolderAsync(filename, parent);
        return true;
    }

    public bool DirectoryExists(string path)
    {
        var n = GetNode(path);
        return n is not null && n.Type is NodeType.Directory;
    }

    public async Task<bool> DirectoryExistsAsync(string path, CancellationToken ct = default)
    {
        var n = await GetNodeAsync(path);
        return n is not null && n.Type is NodeType.Directory;
    }

    public void MoveFile(string path, string newPath, bool overwrite = false)
    {
        var oldnode = GetNode(path) ?? throw new FileNotFoundException($"Could not find file \"{path}\"");

        var newparentpath = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(newPath).Value;
        var newparentnode = GetNode(newparentpath) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{newparentpath}\"");
        var newnode = GetNode(newPath);

        if (newnode is null)
        {
            if (newparentnode.Type is NodeType.Directory or NodeType.Root)
            {
                var moved = Client.Move(oldnode, newparentnode);
                Client.Rename(moved, oldnode.Name);
            }
            else 
                throw new IOException($"Cannot move file under node \"{newparentpath}\" because it's not a directory");
        }
        else
        {
            if (overwrite)
            {
                InvalidateCache();
                Client.Delete(newnode, true);
                Client.Rename(Client.Move(oldnode, newparentnode), oldnode.Name);
            }
            else
                throw new IOException($"Cannot move file node to \"{newPath}\" because a node with the same name already exists under that node");
        }
    }

    public async Task MoveFileAsync(string path, string newPath, bool overwrite = false, CancellationToken ct = default)
    {
        var oldnode = await GetNodeAsync(path) ?? throw new FileNotFoundException($"Could not find file \"{path}\"");

        var newparentpath = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(newPath).Value;
        var newparentnode = await GetNodeAsync(newparentpath) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{newparentpath}\"");
        var newnode = await GetNodeAsync(newPath);

        if (newnode is null)
        {
            if (newparentnode.Type is NodeType.Directory or NodeType.Root)
            {
                InvalidateCache();
                var moved = await Client.MoveAsync(oldnode, newparentnode);
                await Client.RenameAsync(moved, oldnode.Name);
            }
            else
                throw new IOException($"Cannot move file under node \"{newparentpath}\" because it's not a directory");
        }
        else
        {
            if (overwrite)
            {
                InvalidateCache();
                await Client.DeleteAsync(newnode, true);
                await Client.RenameAsync(await Client.MoveAsync(oldnode, newparentnode), newnode.Name);
            }
            else
                throw new IOException($"Cannot move file node to \"{newPath}\" because a node with the same name already exists under that node");
        }
    }

    public void CopyFile(string path, string newPath, bool overwrite = false)
    {
        var oldnode = GetNode(path) ?? throw new FileNotFoundException($"Could not find file \"{path}\"");

        var newparentpath = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(newPath).Value;
        var newparentnode = GetNode(newparentpath) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{newparentpath}\"");
        var newnode = GetNode(newPath);

        if (newnode is null)
        {
            if (newparentnode.Type is NodeType.Directory or NodeType.Root)
            {
                InvalidateCache();
                Client.Upload(Client.Download(oldnode), oldnode.Name, newparentnode);
            }
            else
                throw new IOException($"Cannot copy file under node \"{newparentpath}\" because it's not a directory");
        }
        else
        {
            if (overwrite)
            {
                InvalidateCache();
                Client.Delete(newnode, true);
                Client.Upload(Client.Download(oldnode), newnode.Name, newparentnode);
            }
            else
                throw new IOException($"Cannot move file node to \"{newPath}\" because a node with the same name already exists under that node");
        }
    }

    public async Task CopyFileAsync(string path, string newPath, bool overwrite = false, CancellationToken ct = default)
    {
        var oldnode = await GetNodeAsync(path) ?? throw new FileNotFoundException($"Could not find file \"{path}\"");

        var newparentpath = Regexes.GetPathWithoutFileOrDirectoryNameRegex().Match(newPath).Value;
        var newparentnode = await GetNodeAsync(newparentpath) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{newparentpath}\"");
        var newnode = await GetNodeAsync(newPath);

        if (newnode is null)
        {
            if (newparentnode.Type is NodeType.Directory or NodeType.Root)
            {
                InvalidateCache();
                await Client.UploadAsync(await Client.DownloadAsync(oldnode), oldnode.Name, newparentnode);
            }
            else
                throw new IOException($"Cannot copy file under node \"{newparentpath}\" because it's not a directory");
        }
        else
        {
            if (overwrite)
            {
                InvalidateCache();
                await Client.DeleteAsync(newnode, true);
                await Client.UploadAsync(await Client.DownloadAsync(oldnode), newnode.Name, newparentnode);
            }
            else
                throw new IOException($"Cannot move file node to \"{newPath}\" because a node with the same name already exists under that node");
        }
    }

    public bool DeleteFile(string path)
    {
        var n = GetNode(path) ?? throw new DirectoryNotFoundException($"Could not find the File Node {path}");

        if (n.Type is not NodeType.File)
            throw new InvalidOperationException($"Cannot delete node \"{path}\", because it is not a file; instead it's of type {n.Type}");

        InvalidateCache();
        Client.Delete(n, true);
        return true;
    }

    public async Task<bool> DeleteFileAsync(string path, CancellationToken ct = default)
    {
        var n = await GetNodeAsync(path) ?? throw new DirectoryNotFoundException($"Could not find the File Node {path}");

        if (n.Type is not NodeType.File)
            throw new InvalidOperationException($"Cannot delete node \"{path}\", because it is not a file; instead it's of type {n.Type}");

        InvalidateCache();
        await Client.DeleteAsync(n, true);
        return true;
    }

    public bool FileExists(string path)
    {
        var n = GetNode(path);
        return n is not null && n.Type is NodeType.File;
    }

    public async Task<bool> FileExistsAsync(string path, CancellationToken ct = default)
    {
        var n = await GetNodeAsync(path);
        return n is not null && n.Type is NodeType.File;
    }

    public IEnumerable<string> ListFiles(string? path)
    {
        var n = GetNode(path) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{path}\"");
        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot list files within {path} because it's not a directory");

        foreach (var node in Client.GetNodes(n).Where(x => x.Type == NodeType.File))
            yield return $"{path}/{node.Name}";
    }

    public async IAsyncEnumerable<string> ListFilesAsync(string? path)
    {
        var n = await GetNodeAsync(path) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{path}\"");
        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot list files within {path} because it's not a directory");

        foreach (var node in (await Client.GetNodesAsync(n)).Where(x => x.Type == NodeType.File))
            yield return $"{path}/{node.Name}";
    }

    public IEnumerable<string> ListDirectories(string? path)
    {
        var n = GetNode(path) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{path}\"");
        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot list files within {path} because it's not a directory");

        foreach (var node in Client.GetNodes(n).Where(x => x.Type == NodeType.Directory))
            yield return $"{path}/{node.Name}";
    }

    public async IAsyncEnumerable<string> ListDirectoriesAsync(string? path)
    {
        var n = await GetNodeAsync(path) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{path}\"");
        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot list files within {path} because it's not a directory");

        foreach (var node in (await Client.GetNodesAsync(n)).Where(x => x.Type == NodeType.Directory))
            yield return $"{path}/{node.Name}";
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (LogoutClientOnDispose && Client.IsLoggedIn)
            Client.Logout();
    }

    public async ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);

        if (LogoutClientOnDispose && Client.IsLoggedIn)
            await Client.LogoutAsync();
    }

    public bool TryGetAbsolutePath(string? path, [NotNullWhen(true)] out string? result)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            result = null;
            return false;
        }

        result = PreparePath(path);
        return true;
    }

    public IEnumerable<EntryInfo> ListEntries(string? path = null)
    {
        var n = GetNode(path) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{path}\"");
        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot list files within {path} because it's not a directory");

        foreach (var node in Client.GetNodes(n))
            yield return new($"{path}/{node.Name}", node.Type switch
            {
                NodeType.File => EntryType.File,
                NodeType.Directory => EntryType.Directory,
                NodeType.Root => EntryType.Directory,
                NodeType.Inbox => EntryType.Directory,
                NodeType.Trash => EntryType.Directory,
                _ => throw new NotImplementedException(),
            });
    }

    public async IAsyncEnumerable<EntryInfo> ListEntriesAsync(string? path = null, [EnumeratorCancellation] CancellationToken ct = default)
    {
        var n = await GetNodeAsync(path) ?? throw new DirectoryNotFoundException($"Could not find directory node \"{path}\"");
        if (n.Type is not NodeType.Directory and not NodeType.Root)
            throw new IOException($"Cannot list files within {path} because it's not a directory");

        foreach (var node in (await Client.GetNodesAsync(n)))
            yield return new($"{path}/{node.Name}", node.Type switch
            {
                NodeType.File => EntryType.File,
                NodeType.Directory => EntryType.Directory,
                NodeType.Root => EntryType.Directory,
                NodeType.Inbox => EntryType.Directory,
                NodeType.Trash => EntryType.Directory,
                _ => throw new NotImplementedException(),
            });
    }
}
