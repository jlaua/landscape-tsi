using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class CatalogDbContextMappingTests
{
    [Fact]
    public void LegacyCatalogModel_MapsAllSixteenTablesWithoutMigrationOwnership()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"catalog-map-{Guid.NewGuid():N}").Options;
        using var context = new CatalogDbContext(options);

        var expected = new Dictionary<Type, string>
        {
            [typeof(TmDominio)] = "TMDominio", [typeof(TBuildingBlock)] = "TBuildingBlock",
            [typeof(TCapacidadSeguridad)] = "TCapacidadDeSeguridad", [typeof(TMEstadoCapacidad)] = "TMEstadoCapacidad",
            [typeof(TFuncionalidad)] = "TFuncionalidad", [typeof(TMEstadoFuncionalidad)] = "TMEstadoFuncionalidad",
            [typeof(TEstadoFaseAdopcion)] = "TEstadoFaseAdopcion", [typeof(TTecnologiaTSI)] = "TTecnologiaTSI",
            [typeof(TMFamilia)] = "TMFamilia", [typeof(TCasosDeUso)] = "TCasosDeUso",
            [typeof(TEmpresaSubsidiaria)] = "TEmpresaSubsidiaria", [typeof(TCiso)] = "TCISO",
            [typeof(TMPosturaRoadmap)] = "TMPosturaRoadmap", [typeof(TMEstadoAdopcionTSI)] = "TMEstadoAdopcionTSI",
            [typeof(TModalidadLaboral)] = "TModalidadLaboral", [typeof(TTipoOperacion)] = "TTipoOperacion"
        };

        Assert.Equal(expected.Count, context.Model.GetEntityTypes().Count());
        foreach (var (clrType, table) in expected)
        {
            var entity = context.Model.FindEntityType(clrType)!;
            Assert.Equal(table, entity.GetTableName());
            Assert.Equal("dbo", entity.GetSchema());
        }

        Assert.Equal("nombreEmpresa", context.Model.FindEntityType(typeof(TEmpresaSubsidiaria))!.FindProperty(nameof(TEmpresaSubsidiaria.Nombre))!.GetColumnName());
        Assert.Equal("fechaCompromisoEvaluacionCorporativo", context.Model.FindEntityType(typeof(TTecnologiaTSI))!.FindProperty(nameof(TTecnologiaTSI.FechaEvaluacion))!.GetColumnName());
    }
}
