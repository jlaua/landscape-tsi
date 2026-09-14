using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Domain.Identity;
using Landscape.Tsi.Infrastructure.Adoption;
using Landscape.Tsi.Infrastructure.Catalogs;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Adoption;

public sealed class ServiceManagementTests
{
    private static CatalogDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var ctx = new CatalogDbContext(options);

        // Semillas de tipo de servicio
        ctx.ServiceTypes.AddRange(
            new TTipoServicio { IdTipoServicio = 1, Codigo = "IMPLEMENTACION", Nombre = "Implementación", Orden = 1 },
            new TTipoServicio { IdTipoServicio = 2, Codigo = "MIGRACION", Nombre = "Migración", Orden = 2 },
            new TTipoServicio { IdTipoServicio = 3, Codigo = "OPERACION", Nombre = "Operación", Orden = 3 }
        );

        // Semillas de actividades N1, N2, N3
        for (int i = 1; i <= 8; i++) ctx.SupportLevelActivities.Add(new TActividadNivelSoporte { IdActividadSoporte = i, NivelSoporte = "N1", DescripcionActividad = $"N1 Actividad {i}", OrdenVisual = i });
        for (int i = 1; i <= 12; i++) ctx.SupportLevelActivities.Add(new TActividadNivelSoporte { IdActividadSoporte = 10 + i, NivelSoporte = "N2", DescripcionActividad = $"N2 Actividad {i}", OrdenVisual = i });
        for (int i = 1; i <= 5; i++) ctx.SupportLevelActivities.Add(new TActividadNivelSoporte { IdActividadSoporte = 30 + i, NivelSoporte = "N3", DescripcionActividad = $"N3 Actividad {i}", OrdenVisual = i });

        ctx.SaveChanges();
        return ctx;
    }

    [Fact]
    public async Task GetServiceTypes_ReturnsAllActiveTypes_Ordered()
    {
        using var ctx = CreateInMemoryContext();
        var service = new ServiceManagementService(ctx, new NullAuditTrailService());

        var types = await service.GetServiceTypesAsync();

        Assert.Equal(3, types.Count);
        Assert.Equal("IMPLEMENTACION", types[0].Codigo);
        Assert.Equal("MIGRACION", types[1].Codigo);
        Assert.Equal("OPERACION", types[2].Codigo);
    }

    [Fact]
    public async Task GetSupportActivities_FiltersByLevel_Correctly()
    {
        using var ctx = CreateInMemoryContext();
        var service = new ServiceManagementService(ctx, new NullAuditTrailService());

        var n1Acts = await service.GetSupportActivitiesAsync("N1");
        var n2Acts = await service.GetSupportActivitiesAsync("N2");
        var n3Acts = await service.GetSupportActivitiesAsync("N3");
        var allActs = await service.GetSupportActivitiesAsync();

        Assert.Equal(8, n1Acts.Count);
        Assert.Equal(12, n2Acts.Count);
        Assert.Equal(5, n3Acts.Count);
        Assert.Equal(25, allActs.Count);
    }

    [Fact]
    public async Task SaveServiceHeader_Fails_WhenNoTechnologyAssigned()
    {
        using var ctx = CreateInMemoryContext();
        var service = new ServiceManagementService(ctx, new NullAuditTrailService());

        var cmd = new SaveServiceHeaderCommand(
            null,
            "SRV-TEST-001",
            "Servicio Huérfano",
            null,
            1,
            null, // No tecnología corporativa
            null, // No tecnología implementada
            null,
            null,
            null,
            null,
            "EVALUACION",
            "USD",
            Guid.NewGuid(),
            "CORR-01");

        var result = await service.SaveServiceHeaderAsync(cmd);

        Assert.False(result.Success);
        Assert.Contains("asociarse a una tecnología", result.Message);
    }

    [Fact]
    public async Task SaveProjectRateCard_CalculatesSubtotalAndTotal_Accurately()
    {
        using var ctx = CreateInMemoryContext();
        var service = new ServiceManagementService(ctx, new NullAuditTrailService());

        // 1. Crear servicio de implementación corporativo
        var createSrv = await service.SaveServiceHeaderAsync(new SaveServiceHeaderCommand(
            null,
            "SRV-IMP-001",
            "Despliegue WAAP",
            "Servicio de implementación inicial",
            1,
            10, // IdTecnologiaTSI
            null,
            null,
            100, // IdProcesoAdopcionTSI
            null,
            null,
            "COTIZADO",
            "USD",
            Guid.NewGuid(),
            "CORR-01"));

        Assert.True(createSrv.Success);
        var srvId = createSrv.EntityId!.Value;

        // 2. Agregar tramo: Proyecto menor (1 a 100 horas) - 50 hrs @ .00
        var rate1 = await service.SaveProjectRateCardAsync(new SaveProjectRateCardCommand(
            null,
            srvId,
            "Proyecto menor",
            1,
            100,
            80.00m,
            50.00m,
            "USD",
            "Fase 1 análisis y despliegue",
            Guid.NewGuid(),
            "CORR-01"));

        Assert.True(rate1.Success);

        // 3. Agregar tramo: Proyecto intermedio (101 a 200 horas) - 100 hrs @ .00
        var rate2 = await service.SaveProjectRateCardAsync(new SaveProjectRateCardCommand(
            null,
            srvId,
            "Proyecto intermedio",
            101,
            200,
            65.00m,
            100.00m,
            "USD",
            "Fase 2 integración y políticas",
            Guid.NewGuid(),
            "CORR-01"));

        Assert.True(rate2.Success);

        // 4. Verificar servicio y cálculo de total
        var detail = await service.GetServiceByIdAsync(srvId);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.TarifariosProyecto.Count);

        var subtotal1 = detail.TarifariosProyecto[0].Subtotal; // 50 * 80 = 4,000
        var subtotal2 = detail.TarifariosProyecto[1].Subtotal; // 100 * 65 = 6,500
        Assert.Equal(4000.00m, subtotal1);
        Assert.Equal(6500.00m, subtotal2);
        Assert.Equal(10500.00m, detail.CostoTotalEstimado);
    }

    [Fact]
    public async Task SaveOperationRateCard_SupportsPayPerUseAndMonthly24x7_Accurately()
    {
        using var ctx = CreateInMemoryContext();
        var service = new ServiceManagementService(ctx, new NullAuditTrailService());

        // 1. Crear servicio de operación para subsidiaria
        var createSrv = await service.SaveServiceHeaderAsync(new SaveServiceHeaderCommand(
            null,
            "SRV-OPE-001",
            "Soporte Continuo SOC 24x7",
            "Operación delegada",
            3,
            null,
            55, // IdTecnologiaTSIimplementadaSubsidiaria
            2,  // IdEmpresaSubsidiaria
            null,
            null,
            "Vendor Partner S.A.",
            "ADJUDICADO",
            "USD",
            Guid.NewGuid(),
            "CORR-02"));

        Assert.True(createSrv.Success);
        var srvId = createSrv.EntityId!.Value;

        // 2. Agregar ítem mensual 24x7 N2 Senior Remoto (720 hrs base promedio) - 12 meses @ ,500/mes
        var rate1 = await service.SaveOperationRateCardAsync(new SaveOperationRateCardCommand(
            null,
            srvId,
            "N2",
            "Mensual 24x7",
            "Promedio 720 horas mensuales",
            720,
            "Senior",
            "Remoto",
            null,
            4500.00m,
            12,
            null,
            "USD",
            Guid.NewGuid(),
            "CORR-02"));

        Assert.True(rate1.Success);

        // 3. Agregar ítem Pay-Per-Use N3 Senior Presencial (horas bajo demanda) - 40 hrs @ /h
        var rate2 = await service.SaveOperationRateCardAsync(new SaveOperationRateCardCommand(
            null,
            srvId,
            "N3",
            "Pay-Per-Use",
            "Tarifario por Hora bajo demanda",
            0,
            "Senior",
            "Presencial",
            150.00m,
            null,
            1,
            40.00m,
            "USD",
            Guid.NewGuid(),
            "CORR-02"));

        Assert.True(rate2.Success);

        // 4. Verificar subtotales y total acumulado
        var detail = await service.GetServiceByIdAsync(srvId);
        Assert.NotNull(detail);
        Assert.Equal(2, detail.TarifariosOperacion.Count);

        // Mensual: 12 * 4500 = 54,000
        // Pay-Per-Use: 40 * 150 = 6,000
        // Total esperado: 60,000
        Assert.Equal(60000.00m, detail.CostoTotalEstimado);
    }

    [Fact]
    public async Task DeleteRateCard_RecalculatesTotal_Accurately()
    {
        using var ctx = CreateInMemoryContext();
        var service = new ServiceManagementService(ctx, new NullAuditTrailService());

        var createSrv = await service.SaveServiceHeaderAsync(new SaveServiceHeaderCommand(
            null, "SRV-MIG-001", "Migración", null, 2, 10, null, null, null, null, null, "EVALUACION", "USD", Guid.NewGuid(), "C1"));
        var srvId = createSrv.EntityId!.Value;

        var r1 = await service.SaveProjectRateCardAsync(new SaveProjectRateCardCommand(null, srvId, "Baja (Por Hora)", 0, null, 100m, 10m, "USD", null, Guid.NewGuid(), "C1"));
        var r2 = await service.SaveProjectRateCardAsync(new SaveProjectRateCardCommand(null, srvId, "Proyecto menor", 1, 100, 80m, 20m, "USD", null, Guid.NewGuid(), "C1"));

        var detailBefore = await service.GetServiceByIdAsync(srvId);
        Assert.Equal(2600m, detailBefore!.CostoTotalEstimado); // 10*100 (1000) + 20*80 (1600)

        // Eliminar segundo tramo
        var delResult = await service.DeleteProjectRateCardAsync(r2.EntityId!.Value, Guid.NewGuid(), "C1");
        Assert.True(delResult.Success);

        var detailAfter = await service.GetServiceByIdAsync(srvId);
        Assert.Single(detailAfter!.TarifariosProyecto);
        Assert.Equal(1000m, detailAfter.CostoTotalEstimado);
    }

    private sealed class NullAuditTrailService : IAuditTrailService
    {
        public Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount, string? description = null, CancellationToken cancellationToken = default) => Task.FromResult(new AuditOperation());
        public void AddSnapshot(AuditOperation operation, string entityCode, string physicalTableName, string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder, bool isRoot, string? displayName = null) { }
    }
}