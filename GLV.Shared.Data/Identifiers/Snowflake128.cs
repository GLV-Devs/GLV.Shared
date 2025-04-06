using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GLV.Shared.Data.Identifiers;

[StructLayout(LayoutKind.Explicit)]
public readonly struct Snowflake128 : IEquatable<Snowflake128>, IComparable<Snowflake128>, IParsable<Snowflake128>, IFormattable
{
    public static ushort Snowflake128MachineId { get; set; }

    private static long LastStamp;
    private static ushort LastIndex;

    [FieldOffset(0)]
    private readonly Guid asGuid;

    [FieldOffset(0)]
    private readonly long timeStampUtc;

    [FieldOffset(sizeof(long))]
    private readonly ushort index;

    [FieldOffset(sizeof(long) + sizeof(ushort))]
    private readonly ushort machineId;

    public Snowflake128(long timeStampUtc, ushort index, ushort machineId)
    {
        this.timeStampUtc = timeStampUtc;
        this.index = index;
        this.machineId = machineId;
    }

    public Snowflake128(Guid value)
        => asGuid = value;

    public Snowflake128(UInt128 value)
        => asGuid = MemoryMarshal.Cast<UInt128, Guid>(MemoryMarshal.CreateReadOnlySpan(in value, 1))[0];

    public static long GetSnowflake128TimeStamp()
        => DateTime.UtcNow.Ticks;

    public static Snowflake128 New()
    {
        var stamp = GetSnowflake128TimeStamp();
        if (stamp != LastStamp)
        {
            LastIndex = 0;
            LastStamp = stamp;
        }

        return new Snowflake128(stamp, LastIndex++, Snowflake128MachineId);
    }

    public UInt128 AsUInt128()
        => MemoryMarshal.Cast<Guid, UInt128>(MemoryMarshal.CreateReadOnlySpan(in asGuid, 1))[0];

    public Guid AsGuid() => asGuid;

    public DateTime TimeStamp => new(timeStampUtc, DateTimeKind.Utc);

    public ushort Index => index;

    public ushort MachineId => machineId;

    public bool Equals(Snowflake128 other)
        => asGuid == other.asGuid;

    public int CompareTo(Snowflake128 other)
        => asGuid.CompareTo(other.asGuid);

    public static Snowflake128 Parse(string s, IFormatProvider? provider)
        => new(Guid.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Snowflake128 result)
    {
        if (Guid.TryParse(s, provider, out var value))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => asGuid.ToString(format, formatProvider);

    public override string ToString()
        => ToString(null, null);

    public override bool Equals(object? obj)
        => asGuid.Equals(obj);

    public override int GetHashCode()
        => asGuid.GetHashCode();

    public static bool operator ==(Snowflake128 left, Snowflake128 right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Snowflake128 left, Snowflake128 right)
    {
        return !(left == right);
    }

    public static bool operator <(Snowflake128 left, Snowflake128 right)
    {
        return left.CompareTo(right) < 0;
    }

    public static bool operator <=(Snowflake128 left, Snowflake128 right)
    {
        return left.CompareTo(right) <= 0;
    }

    public static bool operator >(Snowflake128 left, Snowflake128 right)
    {
        return left.CompareTo(right) > 0;
    }

    public static bool operator >=(Snowflake128 left, Snowflake128 right)
    {
        return left.CompareTo(right) >= 0;
    }
}