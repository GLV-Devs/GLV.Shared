namespace GLV.Shared.EntityFrameworkHosting.Options;

public record ServerDatabaseConfiguration(
    DatabaseType DatabaseType,
    string? SQLServerConnectionString = null,
    string? SQLiteConnectionString = null
)
    : DatabaseConfiguration(DatabaseType, SQLServerConnectionString, SQLiteConnectionString);
