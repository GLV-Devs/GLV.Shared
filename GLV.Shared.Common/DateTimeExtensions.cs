namespace GLV.Shared.Common;

public static class DateTimeExtensions
{
    public static string ToFullDateTimeWithTimeZone(this DateTime date, IFormatProvider? format = null)
        => $"{date.ToString("F", format)} UTC{date.ToString("zzz", format)}";

    public static string ToFullDateTimeWithTimeZone(this DateTimeOffset date, IFormatProvider? format = null)
        => $"{date.ToString("F", format)} UTC{date.ToString("zzz", format)}";
}
