namespace Landscape.Tsi.Tests.Identity;

public sealed class ProductionDatabaseSafetyTests
{
    [Fact]
    public void Repository_HasNoAutomaticProductionMigrationOrSqlExecution()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var searchableRoots = new[] { "src", "tests", ".github", "scripts" }
            .Select(path => Path.Combine(repositoryRoot, path))
            .Where(Directory.Exists);
        var files = searchableRoots.SelectMany(root => Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => Path.GetExtension(path) is ".cs" or ".ps1" or ".sh" or ".yml" or ".yaml");

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            if (file.EndsWith(nameof(ProductionDatabaseSafetyTests) + ".cs", StringComparison.Ordinal))
            {
                continue;
            }

            Assert.DoesNotContain("Database.Migrate", source, StringComparison.Ordinal);
            Assert.DoesNotContain("ExecuteSqlRaw", source, StringComparison.Ordinal);
            Assert.DoesNotContain("db-landscape-tsi;", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("database update", source, StringComparison.OrdinalIgnoreCase);
        }
    }
}
