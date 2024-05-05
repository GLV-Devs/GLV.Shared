using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace GLV.Shared.Data.Identifiers;

#if Snowflake128
[StructLayout(LayoutKind.Explicit)]
public readonly struct Snowflake128 : IEquatable<Snowflake128>, IComparable<Snowflake128>, IParsable<Snowflake128>, IFormattable
{
    public static ushort Snowflake128MachineId { get; set; }

    private static long LastStamp;
    private static ushort LastIndex;

    [FieldOffset(0)]
    private readonly UInt128 asUInt128;

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

    public Snowflake128(UInt128 value)
    {
        asUInt128 = value;
    }

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

    public UInt128 AsUInt128() => asUInt128;

    public DateTime TimeStamp => new(timeStampUtc, DateTimeKind.Utc);

    public ushort Index => index;

    public ushort MachineId => machineId;

    public bool Equals(Snowflake128 other)
        => asUInt128 == other.asUInt128;

    public int CompareTo(Snowflake128 other)
        => asUInt128.CompareTo(other.asUInt128);

    public static Snowflake128 Parse(string s, IFormatProvider? provider)
        => new(UInt128.Parse(s, provider));

    public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out Snowflake128 result)
    {
        if (UInt128.TryParse(s, provider, out var value))
        {
            result = new(value);
            return true;
        }

        result = default;
        return false;
    }

    public string ToString(string? format, IFormatProvider? formatProvider)
        => asUInt128.ToString(format, formatProvider);

    public override string ToString()
        => ToString(null, null);

    public override bool Equals(object? obj)
        => asUInt128.Equals(obj);

    public override int GetHashCode()
        => asUInt128.GetHashCode();

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
#endif