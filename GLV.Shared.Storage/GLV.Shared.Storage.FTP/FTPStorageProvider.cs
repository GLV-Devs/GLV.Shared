using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentFTP;
using GLV.Shared.Storage.Abstractions;
using GLV.Shared.Storage.FTP;
using NeoSmart.AsyncLock;
using static GLV.Shared.Storage.FTP.FTPStorageProvider;

namespace GLV.Shared.Storage.FTP;

public partial class FTPStorageProvider(ClientFactory syncFactory, AsyncClientFactory asyncFactory, string? root) : IStorageProvider
{
    public const string ProviderName = "FTP";
    private const int TransferChunkSize = 1024;

    public delegate FtpClient ClientFactory(FTPStorageProvider provider);

    public delegate AsyncFtpClient AsyncClientFactory(FTPStorageProvider provider);

    private static readonly FtpConfig NoCertificateConfig = new()
    {
        RetryAttempts = 5,
        EncryptionMode = FtpEncryptionMode.Auto,
        ValidateAnyCertificate = true
    };

    private static readonly FtpConfig DefaultConfig = new()
    {
        RetryAttempts = 5,
        EncryptionMode = FtpEncryptionMode.Auto
    };

    public string Provider { get; } = ProviderName;

    private readonly AsyncLock ftpprofilelock = new();
    private FtpProfile? ftpprofile;

    public string? Root { get; } = root;

    public FTPStorageProvider(FTPProviderData data, string? root)
        : this(data.Password, data.Username, data.Host, data.Port, root, data.ValidateAnyCertificate) { }

    public FTPStorageProvider(string? password, string? username, string host, ushort port, string? root, bool validateAnyCertificate = false)
        : this(
            p =>
            {
#if DEBUG
                var validateAnyCertificate = true;
#else
                var validateAnyCertificate = validateAnyCertificate;
#endif
                return new FtpClient(host, username, password, port, validateAnyCertificate ? NoCertificateConfig : DefaultConfig);
            },
            p =>
            {
#if DEBUG
                var validateAnyCertificate = true;
#else
                var validateAnyCertificate = validateAnyCertificate;
#endif
                return new AsyncFtpClient(host, username, password, port, validateAnyCertificate ? NoCertificateConfig : DefaultConfig);
            },
            root
        )
    { }

    [return: NotNullIfNotNull(nameof(path))]
    public string? PreparePath(string? path) 
        => (Root is not null && path is not null && path.StartsWith(Root) ? path : Path.Combine(Root ?? "", path ?? "")).Replace('\\', '/');

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
}
