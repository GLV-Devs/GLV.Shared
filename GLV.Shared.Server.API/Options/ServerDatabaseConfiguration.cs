using GLV.Shared.Server.API.Configuration;

namespace GLV.Shared.Server.API.Options;

public record ServerDatabaseConfiguration(
    DatabaseType DatabaseType,
    TimeSpan UserAlertCleanUpInterval,
    string? SQLServerConnectionString = null,
    string? SQLiteConnectionString = null
)
    : DatabaseConfiguration(DatabaseType, SQLServerConnectionString, SQLiteConnectionString);
