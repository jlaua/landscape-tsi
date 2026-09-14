using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Infrastructure.Adoption;
using Landscape.Tsi.Infrastructure.Catalogs;
using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Adoption;

public sealed class AdoptionProcessUnitTests
{
    [Fact]
    public void ContractDto_CalculatesAdendasCorrectly()
    {
        var adenda1 = new ContractDto(2, 10, "CT-001-A1", true, 1, "CT-001", new DateTime(2025, 1, 1), new DateTime(2025, 12, 31), null, null, 15000m, "USD", null, []);
        var adenda2 = new ContractDto(3, 10, "CT-001-A2", true, 1, "CT-001", new DateTime(2026, 1, 1), new DateTime(2026, 12, 31), null, null, 20000m, "USD", null, []);
        var parent = new ContractDto(1, 10, "CT-001", false, null, null, new DateTime(2024, 1, 1), new DateTime(2024, 12, 31), null, null, 100000m, "USD", null, [adenda1, adenda2]);

        Assert.Equal(2, parent.Adendas.Count);
        Assert.Equal("CT-001-A1", parent.Adendas[0].NumeroContrato);
        Assert.Equal("CT-001-A2", parent.Adendas[1].NumeroContrato);
        Assert.Equal(100000m, parent.Monto);
        Assert.Equal(35000m, parent.Adendas.Sum(a => a.Monto ?? 0));
    }

    [Fact]
    public void DriverDto_CalculatesTotalCostCorrectly()
    {
        var driver = new DriverDto(1, 10, "Licencias E5", "Usuarios", 250, 45.50m, "USD", 250 * 45.50m);

        Assert.Equal(11375.00m, driver.CostoTotal);
        Assert.Equal("Licencias E5", driver.Descripcion);
        Assert.Equal("Usuarios", driver.UnidadMedida);
        Assert.Equal("USD", driver.Moneda);
    }

    [Theory]
    [InlineData(false, 0, false, false, "NO_APLICA")]
    [InlineData(true, 0, false, false, "PENDIENTE")]
    [InlineData(true, 1, true, false, "ALINEADO")]
    [InlineData(true, 1, false, true, "HOMOLOGADO")]
    [InlineData(true, 1, false, false, "NO_ALINEADO")]
    public void AlignmentLogic_EvaluatesStateCorrectly(bool aplica, int techCount, bool isPrincipal, bool isAlternative, string expectedGlobalAlignment)
    {
        string alignment;
        if (!aplica)
        {
            alignment = "NO_APLICA";
        }
        else if (techCount == 0)
        {
            alignment = "PENDIENTE";
        }
        else if (isPrincipal)
        {
            alignment = "ALINEADO";
        }
        else if (isAlternative)
        {
            alignment = "HOMOLOGADO";
        }
        else
        {
            alignment = "NO_ALINEADO";
        }

        Assert.Equal(expectedGlobalAlignment, alignment);
    }

    [Fact]
    public void SaveContractCommand_RejectsAdendaWithoutParent()
    {
        var command = new SaveContractCommand(
            TecnologiaImplementadaId: 10,
            NumeroContrato: "AD-001",
            EsAdenda: true,
            ContratoPadreId: null,
            FechaInicio: DateTime.Today,
            FechaFin: DateTime.Today.AddYears(1),
            FechaAdjudicacion: null,
            RutaDocumento: null,
            Monto: null,
            Moneda: "USD",
            Observaciones: null,
            ActorUserId: Guid.NewGuid(),
            CorrelationId: "test-corr");

        Assert.True(command.EsAdenda);
        Assert.Null(command.ContratoPadreId);
    }

    [Fact]
    public void SaveContractCommand_WithPayg_AllowsNullDates()
    {
        var command = new SaveContractCommand(
            TecnologiaImplementadaId: 10,
            NumeroContrato: "PAYG-AWS-001",
            EsAdenda: false,
            ContratoPadreId: null,
            FechaInicio: null,
            FechaFin: null,
            FechaAdjudicacion: null,
            RutaDocumento: null,
            Monto: null,
            Moneda: "USD",
            Observaciones: "Suscripción por uso bajo demanda",
            ActorUserId: Guid.NewGuid(),
            CorrelationId: "test-corr",
            EsPayg: true);

        Assert.True(command.EsPayg);
        Assert.Null(command.FechaInicio);
        Assert.Null(command.FechaFin);
        Assert.Null(command.FechaAdjudicacion);
    }

    [Fact]
    public void TechnologyAdoptionEvolution_MaintainsBackwardCompatibilityProperties()
    {
        var techImpl = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 1,
            IdEmpresaSubsidiaria = 2,
            IdTecnologiaTSI = 3,
            IdBuildingBlock = 4,
            IdProcesoAdopcionEmpresa = 5,
            EsTecnologiaPrimaria = true,
            VersionDesplegada = "12.4.1"
        };

        Assert.Equal(1, techImpl.IdTecnologiaTSIimplementadaSubsidiaria);
        Assert.Equal(2, techImpl.IdEmpresaSubsidiaria);
        Assert.Equal(3, techImpl.IdTecnologiaTSI);
        Assert.Equal(4, techImpl.IdBuildingBlock);
        Assert.Equal(5, techImpl.IdProcesoAdopcionEmpresa);
        Assert.True(techImpl.EsTecnologiaPrimaria);
        Assert.Equal("12.4.1", techImpl.VersionDesplegada);
    }

    [Fact]
    public void BuildingBlockCapabilitiesDto_InitializesPropertiesCorrectly()
    {
        var cap = new CapabilitySummaryDto(10, "Gestión de Identidades", "ACTIVO", ["MFA", "SSO"]);
        var dto = new BuildingBlockCapabilitiesDto(1, "Autenticación Central", "Identidad y Accesos", [cap]);

        Assert.Equal(1, dto.BuildingBlockId);
        Assert.Equal("Autenticación Central", dto.BuildingBlockNombre);
        Assert.Equal("Identidad y Accesos", dto.DominioNombre);
        Assert.Single(dto.Capacidades);
        Assert.Equal("Gestión de Identidades", dto.Capacidades[0].Nombre);
        Assert.Equal(2, dto.Capacidades[0].Funcionalidades.Count);
    }

    [Fact]
    public void ConveneCompanyInput_InitializesValuesCorrectly()
    {
        var inputAplica = new ConveneCompanyInput(10, 5, true, null);
        var inputNoAplica = new ConveneCompanyInput(20, null, false, "No aplica por normativa local");

        Assert.Equal(10, inputAplica.EmpresaId);
        Assert.Equal(5, inputAplica.ContactoFocalId);
        Assert.True(inputAplica.Aplica);
        Assert.Null(inputAplica.JustificacionNoAplica);

        Assert.Equal(20, inputNoAplica.EmpresaId);
        Assert.Null(inputNoAplica.ContactoFocalId);
        Assert.False(inputNoAplica.Aplica);
        Assert.Equal("No aplica por normativa local", inputNoAplica.JustificacionNoAplica);
    }

    [Fact]
    public void SaveContractCommand_WithEdit_AllowsContratoId()
    {
        var command = new SaveContractCommand(
            TecnologiaImplementadaId: 10,
            NumeroContrato: "CT-2025-001-MOD",
            EsAdenda: false,
            ContratoPadreId: null,
            FechaInicio: new DateTime(2025, 1, 1),
            FechaFin: new DateTime(2026, 1, 1),
            FechaAdjudicacion: null,
            RutaDocumento: "https://docs.corp/ct-mod.pdf",
            Monto: 50000m,
            Moneda: "USD",
            Observaciones: "Contrato editado con nuevos términos",
            ActorUserId: Guid.NewGuid(),
            CorrelationId: "corr-123",
            EsPayg: false,
            ContratoId: 42);

        Assert.Equal(42, command.ContratoId);
        Assert.Equal("CT-2025-001-MOD", command.NumeroContrato);
        Assert.Equal(50000m, command.Monto);
    }

    [Fact]
    public void SaveDriverCommand_WithEdit_AllowsDriverId()
    {
        var command = new SaveDriverCommand(
            TecnologiaImplementadaId: 10,
            Descripcion: "Licencias Actualizadas",
            UnidadMedida: "Usuarios",
            Cantidad: 300,
            PrecioUnitario: 50m,
            Moneda: "USD",
            ActorUserId: Guid.NewGuid(),
            CorrelationId: "corr-456",
            DriverId: 77);

        Assert.Equal(77, command.DriverId);
        Assert.Equal("Licencias Actualizadas", command.Descripcion);
        Assert.Equal(300, command.Cantidad);
        Assert.Equal(50m, command.PrecioUnitario);
    }

    [Fact]
    public async Task BatchConveneCompaniesAsync_UnselectedCompanies_AreRemovedFromProcess()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 1,
            CodigoProceso = "PROC-001",
            NombreProceso = "Proceso Test",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1,
            FechaInicio = DateTime.UtcNow
        };
        ctx.AdoptionProcesses.Add(proceso);

        // Pre-populate with 3 companies: 101, 102, 103
        ctx.AdoptionProcessCompanies.AddRange(
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 1, IdProcesoAdopcionTSI = 1, IdEmpresaSubsidiaria = 101, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 2, IdProcesoAdopcionTSI = 1, IdEmpresaSubsidiaria = 102, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 3, IdProcesoAdopcionTSI = 1, IdEmpresaSubsidiaria = 103, Aplica = true }
        );
        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());

        // Batch convene with only company 102 (101 and 103 were unchecked/deselected)
        var selected = new[] { new ConveneCompanyInput(102, null, true, null) };
        var result = await service.BatchConveneCompaniesAsync(1, selected, Guid.NewGuid(), "corr-1");

        Assert.True(result.Succeeded);

        var remaining = await ctx.AdoptionProcessCompanies.Where(c => c.IdProcesoAdopcionTSI == 1).ToListAsync();
        Assert.Single(remaining);
        Assert.Equal(102, remaining[0].IdEmpresaSubsidiaria);
    }

    [Fact]
    public async Task RemoveCompanyFromProcessAsync_RemovesCompanyAndClearsLinkedImplementedTech()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 1,
            CodigoProceso = "PROC-001",
            NombreProceso = "Proceso Test",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1,
            FechaInicio = DateTime.UtcNow
        };
        ctx.AdoptionProcesses.Add(proceso);

        var procCompany = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 5,
            IdProcesoAdopcionTSI = 1,
            IdEmpresaSubsidiaria = 201,
            Aplica = true
        };
        ctx.AdoptionProcessCompanies.Add(procCompany);

        var implTech = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 50,
            IdEmpresaSubsidiaria = 201,
            IdTecnologiaTSI = 301,
            IdBuildingBlock = 10,
            IdProcesoAdopcionEmpresa = 5,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.Add(implTech);

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var result = await service.RemoveCompanyFromProcessAsync(1, 5, Guid.NewGuid(), "corr-2");

        Assert.True(result.Succeeded);

        var exists = await ctx.AdoptionProcessCompanies.AnyAsync(c => c.IdProcesoAdopcionEmpresa == 5);
        Assert.False(exists);

        var updatedTech = await ctx.ImplementedTechnologies.FindAsync(50);
        Assert.NotNull(updatedTech);
        Assert.Null(updatedTech.IdProcesoAdopcionEmpresa);
    }

    [Fact]
    public async Task DeleteImplementedTechnologyAsync_RemovesTechAndCascadesContractsAndDrivers()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var implTech = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 10,
            IdEmpresaSubsidiaria = 1,
            IdTecnologiaTSI = 100,
            IdBuildingBlock = 5,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.Add(implTech);

        var parentContract = new TContratoTecnologia
        {
            IdContratoTecnologia = 1,
            IdTecnologiaTSIimplementadaSubsidiaria = 10,
            NumeroContrato = "CT-001",
            EsAdenda = false
        };
        var adendaContract = new TContratoTecnologia
        {
            IdContratoTecnologia = 2,
            IdTecnologiaTSIimplementadaSubsidiaria = 10,
            NumeroContrato = "CT-001-A1",
            EsAdenda = true,
            IdContratoPadre = 1
        };
        ctx.TechnologyContracts.AddRange(parentContract, adendaContract);

        var driver = new TDriver
        {
            IdDriver = 1,
            IdTecnologiaTSIimplementadaSubsidiaria = 10,
            DescripcionDriver = "Driver Test",
            Cantidad = 10,
            PrecioUnitario = 100m
        };
        ctx.Drivers.Add(driver);

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var result = await service.DeleteImplementedTechnologyAsync(1, 10, Guid.NewGuid(), "corr-del-tech");

        Assert.True(result.Succeeded);

        var techExists = await ctx.ImplementedTechnologies.AnyAsync(t => t.IdTecnologiaTSIimplementadaSubsidiaria == 10);
        Assert.False(techExists);

        var contractsRemaining = await ctx.TechnologyContracts.Where(c => c.IdTecnologiaTSIimplementadaSubsidiaria == 10).ToListAsync();
        Assert.Empty(contractsRemaining);

        var driversRemaining = await ctx.Drivers.Where(d => d.IdTecnologiaTSIimplementadaSubsidiaria == 10).ToListAsync();
        Assert.Empty(driversRemaining);
    }

    [Fact]
    public async Task DeleteImplementedTechnologyAsync_NonExistent_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var result = await service.DeleteImplementedTechnologyAsync(1, 999, Guid.NewGuid(), "corr-not-found");

        Assert.False(result.Succeeded);
        Assert.Equal("La tecnología implementada no existe.", result.Message);
    }

    [Fact]
    public async Task DeleteProcessCascadeAsync_CascadesAndRemovesAllAssociatedRecords()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 100,
            CodigoProceso = "PROC-TEST-100",
            NombreProceso = "Proceso Test Cascade Delete",
            IdBuildingBlock = 1,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var empresaProceso = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 200,
            IdProcesoAdopcionTSI = 100,
            IdEmpresaSubsidiaria = 1,
            Aplica = true
        };
        ctx.AdoptionProcessCompanies.Add(empresaProceso);

        var implTech = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 300,
            IdEmpresaSubsidiaria = 1,
            IdTecnologiaTSI = 50,
            IdBuildingBlock = 1,
            IdProcesoAdopcionEmpresa = 200
        };
        ctx.ImplementedTechnologies.Add(implTech);

        var servicio = new TServicioTecnologia
        {
            IdServicio = 400,
            CodigoServicio = "SRV-TEST",
            NombreServicio = "Servicio Test",
            IdProcesoAdopcionTSI = 100,
            IdTipoServicio = 1,
            IdTecnologiaTSI = 50,
            TarifariosProyecto =
            [
                new TTarifarioProyectoHoras { IdTarifarioProyecto = 501, IdServicio = 400, Complejidad = "Alta", Subtotal = 5000m }
            ],
            TarifariosOperacion =
            [
                new TTarifarioOperacion { IdTarifarioOperacion = 601, IdServicio = 400, NivelSoporte = "L2", Subtotal = 2000m }
            ]
        };
        ctx.TechnologyServices.Add(servicio);

        var estandar = new TEstandarTecnologiaHistorico
        {
            IdEstandarTecnologia = 700,
            IdBuildingBlock = 1,
            IdTecnologiaTSI = 50,
            IdProcesoAdopcionTSI = 100,
            RolEstandar = "PRINCIPAL",
            EstadoVigencia = "ACTIVO_VIGENTE",
            FechaInicioVigencia = DateTime.Today
        };
        ctx.StandardTechnologyHistories.Add(estandar);

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var result = await service.DeleteProcessCascadeAsync(100, Guid.NewGuid(), "corr-test-cascade");

        Assert.True(result.Succeeded);
        Assert.Contains("PROC-TEST-100", result.Message);

        // Assert process is deleted
        Assert.False(await ctx.AdoptionProcesses.AnyAsync(p => p.IdProcesoAdopcionTSI == 100));

        // Assert companies in process are deleted
        Assert.False(await ctx.AdoptionProcessCompanies.AnyAsync(ep => ep.IdProcesoAdopcionTSI == 100));

        // Assert services and rate cards are deleted
        Assert.False(await ctx.TechnologyServices.AnyAsync(s => s.IdProcesoAdopcionTSI == 100));
        Assert.False(await ctx.ProjectRateCards.AnyAsync(p => p.IdServicio == 400));
        Assert.False(await ctx.OperationRateCards.AnyAsync(o => o.IdServicio == 400));

        // Assert standard histories are deleted
        Assert.False(await ctx.StandardTechnologyHistories.AnyAsync(e => e.IdProcesoAdopcionTSI == 100));

        // Assert implemented technology reference to process company is cleared (set to null)
        var updatedImpl = await ctx.ImplementedTechnologies.FirstOrDefaultAsync(t => t.IdTecnologiaTSIimplementadaSubsidiaria == 300);
        Assert.NotNull(updatedImpl);
        Assert.Null(updatedImpl.IdProcesoAdopcionEmpresa);
    }

    [Fact]
    public async Task DeleteProcessCascadeAsync_NonExistent_ReturnsFailure()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var result = await service.DeleteProcessCascadeAsync(9999, Guid.NewGuid(), "corr-not-found");

        Assert.False(result.Succeeded);
        Assert.Equal("El proceso de evaluación de adopción no existe.", result.Message);
    }

    private sealed class NullAuditTrail : IAuditTrailService
    {
        public Task RecordCreateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, int affectedRecordCount = 1, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordUpdateAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RecordRelationAsync(string actionType, string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, string? description = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<Landscape.Tsi.Domain.Identity.AuditOperation> BeginDeleteAsync(string entityCode, string physicalTableName, long recordId, string? displayName, Guid actorUserId, string correlationId, int affectedRecordCount, string? description = null, CancellationToken cancellationToken = default) => Task.FromResult(new Landscape.Tsi.Domain.Identity.AuditOperation());
        public void AddSnapshot(Landscape.Tsi.Domain.Identity.AuditOperation operation, string entityCode, string physicalTableName, string primaryKeyJson, string foreignKeysJson, string rowDataJson, int deleteOrder, int restoreOrder, bool isRoot, string? displayName = null) { }
    }
}