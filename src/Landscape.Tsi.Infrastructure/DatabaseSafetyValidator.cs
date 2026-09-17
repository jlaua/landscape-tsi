using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace Landscape.Tsi.Infrastructure;

internal static class DatabaseSafetyValidator
{
    private const string ExpectedDatabaseKey = "DatabaseSafety:ExpectedDatabaseName";

    public static void Validate(string connectionString, IConfiguration configuration)
    {
        var expectedDatabaseName = configuration[ExpectedDatabaseKey];
        Validate(connectionString, expectedDatabaseName);
    }

    public static void Validate(string connectionString, string? expectedDatabaseName)
    {
        if (string.IsNullOrWhiteSpace(expectedDatabaseName))
        {
            throw new InvalidOperationException(
                "DatabaseSafety:ExpectedDatabaseName debe estar configurado cuando se utiliza una base de datos SQL.");
        }

        var connection = new SqlConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(connection.InitialCatalog)
            || !string.Equals(connection.InitialCatalog, expectedDatabaseName.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "LandscapeTsiDb no coincide con DatabaseSafety:ExpectedDatabaseName.");
        }
    }
}