using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class CatalogModelVerificationTests
{
    [Fact]
    public void CatalogModel_VerifiesDirectOneToManyWithoutBridgeTables()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"catalog-verify-{Guid.NewGuid():N}").Options;
        using var context = new CatalogDbContext(options);

        // 1. TCapacidadDeSeguridad -> TBuildingBlock (1:N nullable FK)
        var capabilityEntity = context.Model.FindEntityType(typeof(TCapacidadSeguridad))!;
        Assert.Equal("TCapacidadDeSeguridad", capabilityEntity.GetTableName());
        var bbForeignKey = capabilityEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TBuildingBlock));
        Assert.NotNull(bbForeignKey);
        Assert.Equal(nameof(TCapacidadSeguridad.IdBuildingBlock), bbForeignKey.Properties.Single().Name);
        Assert.True(bbForeignKey.Properties.Single().IsNullable);
        Assert.Equal("idBuildingBlock", capabilityEntity.FindProperty(nameof(TCapacidadSeguridad.IdBuildingBlock))!.GetColumnName());

        // 2. TFuncionalidad -> TCapacidadDeSeguridad (1:N nullable FK)
        var functionalityEntity = context.Model.FindEntityType(typeof(TFuncionalidad))!;
        Assert.Equal("TFuncionalidad", functionalityEntity.GetTableName());
        var capForeignKey = functionalityEntity.GetForeignKeys()
            .FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TCapacidadSeguridad));
        Assert.NotNull(capForeignKey);
        Assert.Equal(nameof(TFuncionalidad.IdCapacidad), capForeignKey.Properties.Single().Name);
        Assert.True(capForeignKey.Properties.Single().IsNullable);
        Assert.Equal("idCapacidad", functionalityEntity.FindProperty(nameof(TFuncionalidad.IdCapacidad))!.GetColumnName());

        // 3. Confirm DDL_REQUIRED=NO and MIGRATION_REQUIRED=NO: all entities excluded from migrations
        var designTimeModel = context.GetService<IDesignTimeModel>().Model;
        foreach (var entity in designTimeModel.GetEntityTypes())
        {
            Assert.True(entity.IsTableExcludedFromMigrations(),
                $"Entity {entity.ClrType.Name} must be excluded from EF Core migrations.");
        }
    }
}