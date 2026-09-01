using System.Text.RegularExpressions;

namespace Landscape.Tsi.Tests.Identity;

public sealed class IdentityMigrationSafetyTests
{
    [Fact]
    public void InitialMigration_UpOnlyCreatesIamTablesAndDoesNotUseDestructiveOperations()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var migrationsDirectory = Path.Combine(repositoryRoot, "src", "Landscape.Tsi.Infrastructure", "Identity", "Migrations");
        var migrationPath = Directory.GetFiles(migrationsDirectory, "*_InitialIdentityAccess.cs").Single();
        var source = File.ReadAllText(migrationPath);
        var up = source[..source.IndexOf("protected override void Down", StringComparison.Ordinal)];

        Assert.DoesNotContain("DropTable", up, StringComparison.Ordinal);
        Assert.DoesNotContain("DropColumn", up, StringComparison.Ordinal);
        Assert.DoesNotContain("AlterColumn", up, StringComparison.Ordinal);
        Assert.DoesNotContain("Rename", up, StringComparison.Ordinal);
        Assert.DoesNotContain("migrationBuilder.Sql", up, StringComparison.Ordinal);

        var createdTables = Regex.Matches(up, "CreateTable\\(\\s*name: \\\"(?<name>[^\\\"]+)")
            .Select(match => match.Groups["name"].Value)
            .ToArray();

        Assert.NotEmpty(createdTables);
        Assert.All(createdTables, table => Assert.StartsWith("Iam", table, StringComparison.Ordinal));
        Assert.DoesNotContain("CreateTable(\r\n                name: \"TEmpresaSubsidiaria\"", up, StringComparison.Ordinal);
    }
}
