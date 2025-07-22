using System.Buffers;
using System.Runtime.CompilerServices;

namespace GLV.Shared.Common;

public static class ArrayPoolHelper
{
    public readonly struct ArrayPoolRented<T> : IDisposable
    {
        public int Length { get; }
        public T[] Rented { get; }
        public Span<T> Span => Rented.AsSpan(0, Length);

        internal ArrayPoolRented(int minLen)
        {
            Rented = ArrayPool<T>.Shared.Rent(minLen);
            Length = minLen;
        }

        public void Dispose()
        {
            if (Rented is not null)
                ArrayPool<T>.Shared.Return(Rented);
        }
    }

    public static int DefaultLimitForStackAllocationInBytes { get; set; } = 2048;

    private static int GetTotalBytes<T>(int len)
        => Unsafe.SizeOf<T>() * len;

    //public readonly ref struct ArrayPoolRentedOrStackAllocated<T> : IDisposable
    //    where T : unmanaged
    //{
    //    public T[]? Rented { get; }
    //    public Span<T> Span { get; }

    //    internal ArrayPoolRentedOrStackAllocated(int minLen, int maxBeforeHeapAlloc)
    //    {
    //        if (GetTotalBytes<T>(minLen) > maxBeforeHeapAlloc)
    //        {
    //            Rented = ArrayPool<T>.Shared.Rent(minLen);
    //            Span = Rented.AsSpan(0, minLen);
    //        }
    //        else
    //        {
    //            Rented = null;
    //            Span = stackalloc T[minLen];
    //        }
    //    }

    //    public void Dispose()
    //    {
    //        if (Rented is not null)
    //            ArrayPool<T>.Shared.Return(Rented);
    //    }
    //}

    public static ArrayPoolRented<T> Rent<T>(int minimumLength)
        => new(minimumLength);

    public static bool TrySkipRent<T>(int minimumLength, out ArrayPoolRented<T> rented, int? maxBytes = default) where T : unmanaged
    {
        if (GetTotalBytes<T>(minimumLength) > (maxBytes ?? DefaultLimitForStackAllocationInBytes))
        {
            rented = new(minimumLength);
            return false;
        }
        else
        {
            rented = default;
            return true;
        }
    }
}
