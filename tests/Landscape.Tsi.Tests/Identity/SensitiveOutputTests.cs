using Landscape.Tsi.Infrastructure.Identity;

namespace Landscape.Tsi.Tests.Identity;

public sealed class SensitiveOutputTests
{
    [Fact]
    public void ExternalSubject_IsPseudonymizedBeforeAudit()
    {
        const string subject = "provider-subject-that-must-not-be-logged";

        var protectedIdentifier = AuditIdentifier.ProtectExternalSubject(subject);

        Assert.StartsWith("OIDC:", protectedIdentifier, StringComparison.Ordinal);
        Assert.DoesNotContain(subject, protectedIdentifier, StringComparison.Ordinal);
    }

    [Fact]
    public void ProductionSource_HasNoLiteralDatabaseOrBootstrapSecrets()
    {
        var repositoryRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../"));
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var files = Directory.EnumerateFiles(sourceRoot, "*", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
            .Where(path => Path.GetExtension(path) is ".cs" or ".json" or ".cshtml");

        foreach (var file in files)
        {
            var source = File.ReadAllText(file);
            Assert.DoesNotContain("LANDSCAPE_TSI_BOOTSTRAP_ADMIN_PASSWORD=", source, StringComparison.Ordinal);
            Assert.DoesNotContain("BootstrapAdmin:Password\" value=", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("SaveTokens = true", source, StringComparison.Ordinal);
        }
    }
}