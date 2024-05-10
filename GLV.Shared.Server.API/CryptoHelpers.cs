using System.Buffers.Text;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Server.API;

public static class CryptoHelpers
{
    /// <summary>
    /// The Aes encryptor used for command tokens. The key is generated anew each time this class is called for the first time, in its static constructor (which means once per run, at most)
    /// </summary>
    /// <remarks>
    /// Generally, you shouldn't be messing with this. I left it public in case you want to perform some shenanigans you probably should think twice about
    /// </remarks>
    public static Aes CommandTokenEncryptor { get; }

    static CryptoHelpers()
    {
        CommandTokenEncryptor = Aes.Create();
        CommandTokenEncryptor.GenerateKey();
    }

    public static string EncodeCommandToken(string command, TimeSpan delayBeforeBeingValid = default, DateTimeOffset? expiration = null)
        => EncodeCommandToken(command, expiration ?? DateTimeOffset.Now + TimeSpan.FromMinutes(15), DateTimeOffset.Now + delayBeforeBeingValid);

    public static string EncodeCommandToken(string command, DateTimeOffset expiration, DateTimeOffset? validFrom = null)
    {
        var scount = Encoding.UTF8.GetByteCount(command);
        Span<byte> preEncrypt = stackalloc byte[sizeof(long) * 2 + scount];

        MemoryMarshal.Write(preEncrypt[0..8], expiration.UtcTicks);
        MemoryMarshal.Write(preEncrypt[8..16], (validFrom ?? DateTimeOffset.Now).UtcTicks);
        Encoding.UTF8.GetBytes(command, preEncrypt[16..]);

        var msgSize = preEncrypt.Length + 16 - preEncrypt.Length % 16;
        Span<byte> encrypted = stackalloc byte[msgSize + 16 + 4];

        CommandTokenEncryptor.GenerateIV();
        var iv = CommandTokenEncryptor.IV;
        CommandTokenEncryptor.EncryptCbc(preEncrypt, iv, encrypted[(16 + 4)..]);
        iv.CopyTo(encrypted[4..]);
        MemoryMarshal.Write(encrypted[..4], preEncrypt.Length);

        return Convert.ToBase64String(encrypted);
    }

    public static bool TryDecodeCommandToken(ReadOnlySpan<char> token, [NotNullWhen(true)] out string? command, out DateTimeOffset expiration, out DateTimeOffset validFrom)
    {
        var scount = Encoding.UTF8.GetByteCount(token);
        Span<byte> tokenBytes = stackalloc byte[scount];
        Encoding.UTF8.GetBytes(token, tokenBytes);

        int b64count = Base64.GetMaxDecodedFromUtf8Length(scount);
        Span<byte> rawBytes = stackalloc byte[b64count];

        var b64Result = Base64.DecodeFromUtf8(tokenBytes, rawBytes, out _, out int written);
        if (b64Result is not System.Buffers.OperationStatus.Done)
            return Fail(out command, out expiration, out validFrom);

        int len = MemoryMarshal.Read<int>(rawBytes[..4]);
        if (len is <= 0 || len > rawBytes.Length - (16 + 4))
            return Fail(out command, out expiration, out validFrom);

        Span<byte> decrypted = stackalloc byte[len];
        try
        {
            CommandTokenEncryptor.DecryptCbc(rawBytes[(16 + 4)..written], rawBytes[4..(16 + 4)], decrypted);
        }
        catch (Exception)
        {
            return Fail(out command, out expiration, out validFrom);
        }

        expiration = new DateTimeOffset(MemoryMarshal.Read<long>(decrypted[0..8]), TimeSpan.Zero);
        validFrom = new DateTimeOffset(MemoryMarshal.Read<long>(decrypted[8..16]), TimeSpan.Zero);
        command = Encoding.UTF8.GetString(decrypted[16..]);

        return true;

        static bool Fail(out string? command, out DateTimeOffset expiration, out DateTimeOffset validFrom)
        {
            command = null;
            expiration = default;
            validFrom = default;
            return false;
        }
    }

    public static void DecodeCommandToken(ReadOnlySpan<char> token, out string command, out DateTimeOffset expiration, out DateTimeOffset validFrom)
    {
        var scount = Encoding.UTF8.GetByteCount(token);
        Span<byte> tokenBytes = stackalloc byte[scount];
        Encoding.UTF8.GetBytes(token, tokenBytes);

        int b64count = Base64.GetMaxDecodedFromUtf8Length(scount);
        Span<byte> rawBytes = stackalloc byte[b64count];

        var b64Result = Base64.DecodeFromUtf8(tokenBytes, rawBytes, out _, out int written);
        Debug.Assert(b64Result is System.Buffers.OperationStatus.Done);
        int len = MemoryMarshal.Read<int>(rawBytes[..4]);
        if (len is <= 0 or > 200 || len > rawBytes.Length - (16 + 4))
            throw new InvalidDataException($"Could not read a valid length from token. This may mean that the message was corrupted. Read Length: {len}");

        Span<byte> decrypted = stackalloc byte[len];
        CommandTokenEncryptor.DecryptCbc(rawBytes[(16 + 4)..written], rawBytes[4..(16 + 4)], decrypted);

        expiration = new DateTimeOffset(MemoryMarshal.Read<long>(decrypted[0..8]), TimeSpan.Zero);
        validFrom = new DateTimeOffset(MemoryMarshal.Read<long>(decrypted[8..16]), TimeSpan.Zero);
        command = Encoding.UTF8.GetString(decrypted[16..]);
    }
}
