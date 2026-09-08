using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Tests.Infrastructure;

internal static class SqlIntegrationTestSettings
{
    public const string ExpectedDatabaseName = "db-landscape-tsi-dev-v2";

    private static IConfiguration Configuration => new ConfigurationBuilder()
        .AddJsonFile("appsettings.json", optional: true)
        .AddJsonFile("appsettings.Development.json", optional: true)
        .AddUserSecrets(typeof(Program).Assembly, optional: true)
        .AddEnvironmentVariables()
        .Build();

    public static string ConnectionString =>
        Configuration.GetConnectionString("LandscapeTsiDb")
        ?? throw new InvalidOperationException("Database safety validation failed. ConnectionStrings:LandscapeTsiDb is not configured.");

    public static async Task<SqlConnection> OpenValidatedConnectionAsync(CancellationToken cancellationToken = default)
    {
        if (!string.Equals(
                Configuration["DatabaseSafety:ExpectedDatabaseName"],
                ExpectedDatabaseName,
                StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Database safety validation failed.");
        }

        var connection = new SqlConnection(ConnectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
        }
        catch (SqlException exception)
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Database connection failed before DB_NAME validation.", exception);
        }
        await using var command = new SqlCommand("SELECT DB_NAME();", connection);
        var actualDatabase = Convert.ToString(await command.ExecuteScalarAsync(cancellationToken));
        if (!string.Equals(actualDatabase, ExpectedDatabaseName, StringComparison.Ordinal))
        {
            await connection.DisposeAsync();
            throw new InvalidOperationException("Database safety validation failed.");
        }

        return connection;
    }
}
