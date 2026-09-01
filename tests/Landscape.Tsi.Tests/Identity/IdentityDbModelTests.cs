using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Identity;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Landscape.Tsi.Tests.Identity;

public sealed class IdentityDbModelTests
{
    private static IModel CreateModel()
    {
        var options = new DbContextOptionsBuilder<IdentityDbContext>()
            .UseInMemoryDatabase($"model-{Guid.NewGuid()}")
            .Options;
        using var context = new IdentityDbContext(options);
        return context.GetService<IDesignTimeModel>().Model;
    }

    [Fact]
    public void Model_UsesUniqueStableExternalIdentity()
    {
        var entity = CreateModel().FindEntityType(typeof(IamUsuarioLoginExterno));

        Assert.NotNull(entity);
        Assert.Contains(entity.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(x => x.Name).SequenceEqual(["Issuer", "Subject"]));
    }

    [Fact]
    public void Model_ReferencesExistingOrganizationWithoutOwningItsMigration()
    {
        var model = CreateModel();
        var reference = model.FindEntityType(typeof(EmpresaSubsidiariaReference));
        var assignment = model.FindEntityType(typeof(IamUsuarioOrganizacion));

        Assert.NotNull(reference);
        Assert.True(reference.IsTableExcludedFromMigrations());
        Assert.Contains(assignment!.GetForeignKeys(), key => key.PrincipalEntityType == reference);
    }

    [Fact]
    public void Model_IndexesEveryIdentityForeignKey()
    {
        var model = CreateModel();
        var identityEntities = model.GetEntityTypes().Where(x =>
            x.GetTableName()?.StartsWith("Iam", StringComparison.Ordinal) == true);

        foreach (var foreignKey in identityEntities.SelectMany(x => x.GetForeignKeys()))
        {
            var coveredByIndex = foreignKey.DeclaringEntityType.GetIndexes().Any(index =>
                foreignKey.Properties.All(index.Properties.Contains));
            var coveredByKey = foreignKey.DeclaringEntityType.GetKeys().Any(key =>
                foreignKey.Properties.All(key.Properties.Contains));

            Assert.True(
                coveredByIndex || coveredByKey,
                $"FK sin índice: {foreignKey.DeclaringEntityType.DisplayName()}({string.Join(",", foreignKey.Properties.Select(x => x.Name))})");
        }
    }
}
