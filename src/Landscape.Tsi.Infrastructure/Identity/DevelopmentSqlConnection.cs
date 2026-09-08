using Microsoft.Data.SqlClient;

namespace Landscape.Tsi.Infrastructure.Identity;

public static class DevelopmentSqlConnection
{
    public const string ExpectedDatabaseEnvironmentVariable = "DatabaseSafety__ExpectedDatabaseName";

    private static readonly string[] VariableNames =
    [
        "LANDSCAPE_TSI_DEV_SQL_SERVER",
        "LANDSCAPE_TSI_DEV_SQL_DATABASE",
        "LANDSCAPE_TSI_DEV_SQL_USER",
        "LANDSCAPE_TSI_DEV_SQL_PASSWORD"
    ];

    public static string? FromEnvironment() => FromSource(Environment.GetEnvironmentVariable);

    public static string? FromSource(Func<string, string?> valueSource, string? expectedDatabaseName = null)
    {
        var values = VariableNames.ToDictionary(name => name, valueSource, StringComparer.Ordinal);
        if (values.Values.All(string.IsNullOrWhiteSpace))
        {
            return null;
        }

        var missing = values.Where(item => string.IsNullOrWhiteSpace(item.Value)).Select(item => item.Key).ToArray();
        if (missing.Length > 0)
        {
            throw new InvalidOperationException($"Configuración SQL de desarrollo incompleta. Faltan: {string.Join(", ", missing)}.");
        }

        var database = values["LANDSCAPE_TSI_DEV_SQL_DATABASE"]!;
        var configuredExpectedDatabase = expectedDatabaseName
            ?? valueSource(ExpectedDatabaseEnvironmentVariable);
        DatabaseSafetyValidator.Validate(
            new SqlConnectionStringBuilder { InitialCatalog = database }.ConnectionString,
            configuredExpectedDatabase);

        return new SqlConnectionStringBuilder
        {
            DataSource = values["LANDSCAPE_TSI_DEV_SQL_SERVER"],
            InitialCatalog = database,
            UserID = values["LANDSCAPE_TSI_DEV_SQL_USER"],
            Password = values["LANDSCAPE_TSI_DEV_SQL_PASSWORD"],
            Encrypt = true,
            TrustServerCertificate = false,
            PersistSecurityInfo = false
        }.ConnectionString;
    }
}