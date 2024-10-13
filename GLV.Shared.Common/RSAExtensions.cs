using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Common;

public static class RSAExtensions
{
    public static string EncryptAsBase64(this RSA rsa, string plainNonB64Text)
    {
        Span<byte> utf8 = stackalloc byte[Encoding.UTF8.GetByteCount(plainNonB64Text)];
        Encoding.UTF8.TryGetBytes(plainNonB64Text, utf8, out _);
        var cipherText = rsa.Encrypt(utf8, RSAEncryptionPadding.Pkcs1);

        Span<byte> base64 = stackalloc byte[Base64.GetMaxEncodedToUtf8Length(cipherText.Length)];
        Base64.EncodeToUtf8(cipherText, base64, out _, out var written);

        return Encoding.UTF8.GetString(base64);
    }
}
