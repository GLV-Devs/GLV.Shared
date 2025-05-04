namespace GLV.Shared.EntityFramework;

public record class DatabaseConfiguration(DatabaseType DatabaseType, string? SQLServerConnectionString, string? SQLiteConnectionString)
{
    public static string FormatConnectionString(string input, string? subfolder = null)
    {
        ArgumentNullException.ThrowIfNull(input);
        return input.Replace(
                "{appdata}",
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                StringComparison.OrdinalIgnoreCase
            ).Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar);
    }
}
