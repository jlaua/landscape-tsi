using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Catalogs;

public sealed class CatalogDbContextMappingTests
{
    [Fact]
    public void CatalogModel_MapsAllEntitiesWithoutMigrationOwnership()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"catalog-map-{Guid.NewGuid():N}").Options;
        using var context = new CatalogDbContext(options);

        var expected = new Dictionary<Type, string>
        {
            [typeof(TmDominio)] = "TMDominio",
            [typeof(TBuildingBlock)] = "TBuildingBlock",
            [typeof(TCapacidadSeguridad)] = "TCapacidadDeSeguridad",
            [typeof(TMEstadoCapacidad)] = "TMEstadoCapacidad",
            [typeof(TFuncionalidad)] = "TFuncionalidad",
            [typeof(TMEstadoFuncionalidad)] = "TMEstadoFuncionalidad",
            [typeof(TEstadoFaseAdopcion)] = "TEstadoFaseAdopcion",
            [typeof(TTecnologiaTSI)] = "TTecnologiaTSI",
            [typeof(TMFamilia)] = "TMFamilia",
            [typeof(TCasosDeUso)] = "TCasosDeUso",
            [typeof(TEmpresaSubsidiaria)] = "TEmpresaSubsidiaria",
            [typeof(TCiso)] = "TCISO",
            [typeof(TMPosturaRoadmap)] = "TMPosturaRoadmap",
            [typeof(TMEstadoAdopcionTSI)] = "TMEstadoAdopcionTSI",
            [typeof(TModalidadLaboral)] = "TModalidadLaboral",
            [typeof(TTipoOperacion)] = "TTipoOperacion",
            // Nuevas entidades
            [typeof(TProcesoAdopcionTSI)] = "TProcesoAdopcionTSI",
            [typeof(TProcesoAdopcionEmpresa)] = "TProcesoAdopcionEmpresa",
            [typeof(TEstandarTecnologiaHistorico)] = "TEstandarTecnologiaHistorico",
            [typeof(TContratoTecnologia)] = "TContratoTecnologia",
            [typeof(TTecnologiaTSIimplementadaSubsidiaria)] = "TTecnologiaTSIimplementadaSubsidiaria",
            [typeof(TDriver)] = "TDriver",
            // Entidades de Servicios y Tarifarios
            [typeof(TTipoServicio)] = "TTipoServicio",
            [typeof(TServicioTecnologia)] = "TServicioTecnologia",
            [typeof(TTarifarioProyectoHoras)] = "TTarifarioProyectoHoras",
            [typeof(TActividadNivelSoporte)] = "TActividadNivelSoporte",
            [typeof(TTarifarioOperacion)] = "TTarifarioOperacion",
            // Entidades de Fabricantes y Contactos
            [typeof(TVendor)] = "TVendor",
            [typeof(TContactoPartner)] = "TContactoPartner",
            [typeof(TContactoVendor)] = "TContactoVendor"
        };

        Assert.Equal(expected.Count, context.Model.GetEntityTypes().Count());
        foreach (var (clrType, table) in expected)
        {
            var entity = context.Model.FindEntityType(clrType)!;
            Assert.Equal(table, entity.GetTableName());
            Assert.Equal("dbo", entity.GetSchema());
        }

        // Verificaciones de propiedades específicas
        Assert.Equal("nombreEmpresa", context.Model.FindEntityType(typeof(TEmpresaSubsidiaria))!.FindProperty(nameof(TEmpresaSubsidiaria.Nombre))!.GetColumnName());
        Assert.Equal("fechaCompromisoEvaluacionCorporativo", context.Model.FindEntityType(typeof(TTecnologiaTSI))!.FindProperty(nameof(TTecnologiaTSI.FechaEvaluacion))!.GetColumnName());
        Assert.Equal("idFamilia", context.Model.FindEntityType(typeof(TBuildingBlock))!.FindProperty(nameof(TBuildingBlock.IdFamilia))!.GetColumnName());
        Assert.Equal("codigoProceso", context.Model.FindEntityType(typeof(TProcesoAdopcionTSI))!.FindProperty(nameof(TProcesoAdopcionTSI.CodigoProceso))!.GetColumnName());
        Assert.Equal("numeroContrato", context.Model.FindEntityType(typeof(TContratoTecnologia))!.FindProperty(nameof(TContratoTecnologia.NumeroContrato))!.GetColumnName());
        Assert.Equal("montoAnual", context.Model.FindEntityType(typeof(TContratoTecnologia))!.FindProperty(nameof(TContratoTecnologia.MontoAnual))!.GetColumnName());
        Assert.Equal("montoTrianual", context.Model.FindEntityType(typeof(TContratoTecnologia))!.FindProperty(nameof(TContratoTecnologia.MontoTrianual))!.GetColumnName());
        Assert.Equal("precioUnitario", context.Model.FindEntityType(typeof(TDriver))!.FindProperty(nameof(TDriver.PrecioUnitario))!.GetColumnName());
    }

    [Fact]
    public void CatalogModel_ForeignKeys_ConfiguredCorrectlyForNewEntities()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"catalog-fks-{Guid.NewGuid():N}").Options;
        using var context = new CatalogDbContext(options);

        // BuildingBlock -> Familia
        var bbEntity = context.Model.FindEntityType(typeof(TBuildingBlock))!;
        var bbFamFk = bbEntity.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TMFamilia));
        Assert.NotNull(bbFamFk);

        // ProcesoAdopcion -> BuildingBlock
        var procesoEntity = context.Model.FindEntityType(typeof(TProcesoAdopcionTSI))!;
        var procBbFk = procesoEntity.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TBuildingBlock));
        Assert.NotNull(procBbFk);

        // Contrato -> Implementada
        var contratoEntity = context.Model.FindEntityType(typeof(TContratoTecnologia))!;
        var contratoImplFk = contratoEntity.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TTecnologiaTSIimplementadaSubsidiaria));
        Assert.NotNull(contratoImplFk);

        // Driver -> Implementada
        var driverEntity = context.Model.FindEntityType(typeof(TDriver))!;
        var driverImplFk = driverEntity.GetForeignKeys().FirstOrDefault(fk => fk.PrincipalEntityType.ClrType == typeof(TTecnologiaTSIimplementadaSubsidiaria));
        Assert.NotNull(driverImplFk);
    }
}