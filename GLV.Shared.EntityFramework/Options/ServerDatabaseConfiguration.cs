namespace GLV.Shared.EntityFramework.Options;

public record ServerDatabaseConfiguration(
    DatabaseType DatabaseType,
    string? SQLServerConnectionString = null,
    string? SQLiteConnectionString = null
)
    : DatabaseConfiguration(DatabaseType, SQLServerConnectionString, SQLiteConnectionString);
