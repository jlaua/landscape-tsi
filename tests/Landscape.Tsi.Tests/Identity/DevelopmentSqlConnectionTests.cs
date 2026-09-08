using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.Data.SqlClient;

namespace Landscape.Tsi.Tests.Identity;

public sealed class DevelopmentSqlConnectionTests
{
    [Fact]
    public void FromSource_BuildsConfiguredDevelopmentConnectionWithoutPersistingSecurityInfo()
    {
        var values = ValidValues();

        var connectionString = DevelopmentSqlConnection.FromSource(
            name => values.GetValueOrDefault(name),
            "db-landscape-tsi-dev-v2");
        var parsed = new SqlConnectionStringBuilder(connectionString);

        Assert.Equal("db-landscape-tsi-dev-v2", parsed.InitialCatalog);
        Assert.False(parsed.PersistSecurityInfo);
        Assert.True(parsed.Encrypt);
    }

    [Theory]
    [InlineData("db-landscape-tsi")]
    [InlineData("db-landscape-tsi-dev")]
    [InlineData("another-database")]
    public void FromSource_BlocksEveryDatabaseExceptConfiguredDatabase(string database)
    {
        var values = ValidValues();
        values["LANDSCAPE_TSI_DEV_SQL_DATABASE"] = database;

        var error = Assert.Throws<InvalidOperationException>(() =>
            DevelopmentSqlConnection.FromSource(
                name => values.GetValueOrDefault(name),
                "db-landscape-tsi-dev-v2"));

        Assert.DoesNotContain(values["LANDSCAPE_TSI_DEV_SQL_PASSWORD"], error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FromSource_RejectsPartialConfigurationWithoutExposingPresentValues()
    {
        var values = ValidValues();
        values.Remove("LANDSCAPE_TSI_DEV_SQL_USER");

        var error = Assert.Throws<InvalidOperationException>(() =>
            DevelopmentSqlConnection.FromSource(
                name => values.GetValueOrDefault(name),
                "db-landscape-tsi-dev-v2"));

        Assert.Contains("LANDSCAPE_TSI_DEV_SQL_USER", error.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(values["LANDSCAPE_TSI_DEV_SQL_PASSWORD"], error.Message, StringComparison.Ordinal);
    }

    private static Dictionary<string, string> ValidValues() => new(StringComparer.Ordinal)
    {
        ["LANDSCAPE_TSI_DEV_SQL_SERVER"] = "sql.example.test",
        ["LANDSCAPE_TSI_DEV_SQL_DATABASE"] = "db-landscape-tsi-dev-v2",
        ["LANDSCAPE_TSI_DEV_SQL_USER"] = "development-user",
        ["LANDSCAPE_TSI_DEV_SQL_PASSWORD"] = "not-a-real-secret"
    };
}
