using System.Buffers;
using System.Security.Cryptography;
using System.Text;

namespace GLV.Shared.Data;

public static class SignatureHelpers
{
    public static void SignHttpRequest(
        HttpRequestMessage request,
        Span<byte> content,
        Span<byte> output,
        ReadOnlySpan<byte> signatureKey
    ) => SignHttpMessage(
        request.Headers,
        request.Method.Method,
        request.RequestUri?.ToString(),
        content,
        output,
        signatureKey
    );

    public static void SignHttpMessage(
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers, 
        string method,
        string? uri,
        Span<byte> content, 
        Span<byte> output, 
        ReadOnlySpan<byte> signatureKey
    )
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(output.Length, SHA512.HashSizeInBytes);
        int totalSize = 0;
        foreach (var (k, h) in headers)
            foreach (var v in h)
            {
                totalSize += Encoding.UTF8.GetByteCount(k);
                totalSize += Encoding.UTF8.GetByteCount(v);
            }

        totalSize += Encoding.UTF8.GetByteCount(method);
        if (string.IsNullOrWhiteSpace(uri) is false)
            totalSize += Encoding.UTF8.GetByteCount(uri);

        totalSize += content.Length;

        byte[]? rented = null;
        Span<byte> buffer = totalSize > 2048 ? rented = ArrayPool<byte>.Shared.Rent(totalSize) : stackalloc byte[totalSize];
        try
        {
            int wop;
            int written = 0;
            foreach (var (k, h) in headers)
                foreach (var v in h)
                {
                    Encoding.UTF8.TryGetBytes(k, buffer[written..], out wop);
                    written += wop;

                    Encoding.UTF8.TryGetBytes(v, buffer[written..], out wop);
                    written += wop;
                }

            Encoding.UTF8.TryGetBytes(method, buffer[written..], out wop);
            written += wop;

            if (string.IsNullOrWhiteSpace(uri) is false)
            {
                Encoding.UTF8.TryGetBytes(uri, buffer[written..], out wop);
                written += wop;
            }

            content.CopyTo(buffer[written..]);
            written += wop;

            SignMessage(buffer, output, signatureKey);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }

    public static void SignMessage(ReadOnlySpan<byte> input, Span<byte> output, ReadOnlySpan<byte> signatureKey)
    {
        ArgumentOutOfRangeException.ThrowIfNotEqual(output.Length, SHA512.HashSizeInBytes);

        var blen = input.Length + signatureKey.Length;
        byte[]? rented = null;
        Span<byte> buffer = blen > 2048 ? rented = ArrayPool<byte>.Shared.Rent(blen) : stackalloc byte[blen];
        try
        {
            var inputHalf = input.Length / 2;
            input[..inputHalf].CopyTo(buffer);
            signatureKey.CopyTo(buffer[inputHalf..]);
            input[inputHalf..].CopyTo(buffer[(signatureKey.Length + inputHalf)..]);

            SHA512.HashData(input, output);
        }
        finally
        {
            if (rented is not null)
                ArrayPool<byte>.Shared.Return(rented);
        }
    }
}
