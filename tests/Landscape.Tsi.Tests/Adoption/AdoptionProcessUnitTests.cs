using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Identity;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Domain.Catalogs;
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

    [Fact]
    public async Task GetEvaluationReportsAsync_ExtractsRealVendorAndPartner_WithoutSyntheticMockData()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 120,
            CodigoProceso = "PROC-120",
            NombreProceso = "Evaluación WAAP 3",
            IdBuildingBlock = 1,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var bb = new TBuildingBlock { Id = 1, Nombre = "Web Application Firewall" };
        ctx.BuildingBlocks.Add(bb);

        var emp = new TEmpresaSubsidiaria { Id = 10, Nombre = "Mibanco", Pais = "Peru" };
        ctx.Companies.Add(emp);

        var tech = new TTecnologiaTSI { Id = 50, NombreCorporativo = "Akamai App & API Protector" };
        ctx.Technologies.Add(tech);

        var empresaProceso = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 300,
            IdProcesoAdopcionTSI = 120,
            IdEmpresaSubsidiaria = 10,
            Aplica = true
        };
        ctx.AdoptionProcessCompanies.Add(empresaProceso);

        var implTech = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 500,
            IdEmpresaSubsidiaria = 10,
            IdTecnologiaTSI = 50,
            IdBuildingBlock = 1,
            IdProcesoAdopcionEmpresa = 300,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.Add(implTech);

        var contract = new TContratoTecnologia
        {
            IdContratoTecnologia = 600,
            IdTecnologiaTSIimplementadaSubsidiaria = 500,
            NumeroContrato = "CT-WAAP-001",
            Observaciones = "Vendor: AKAMAI / Partner: Apukay",
            MontoContratado = 36000m,
            MontoAnual = 12000m,
            MontoTrianual = 36000m,
            Moneda = "USD"
        };
        ctx.TechnologyContracts.Add(contract);

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var report = await service.GetEvaluationReportsAsync(120);

        Assert.NotNull(report);
        Assert.Single(report.AlcanceReport);
        var row = report.AlcanceReport[0];
        Assert.Equal("Mibanco", row.EmpresaNombre);
        Assert.Equal("AKAMAI", row.Vendor);
        Assert.Equal("Apukay", row.Partner);
        Assert.DoesNotContain("Noventiq", row.Partner ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Telefonica Tech", row.Partner ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Logicalis", row.Partner ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetEvaluationReportsAsync_WhenBuildingBlockIsDspm_DoesNotGenerateSyntheticWaapDrivers_AndIncludesGenericDrivers()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 776,
            CodigoProceso = "EVAL-TSI-20260916-776",
            NombreProceso = "Evaluacion DSPM - Corporativo",
            IdBuildingBlock = 2,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var bb = new TBuildingBlock { Id = 2, Nombre = "DSPM" };
        ctx.BuildingBlocks.Add(bb);

        var emp1 = new TEmpresaSubsidiaria { Id = 1, Nombre = "Banco de Crédito del Perú", Pais = "Perú" };
        var emp2 = new TEmpresaSubsidiaria { Id = 2, Nombre = "Mi Banco Perú", Pais = "Perú" };
        ctx.Companies.AddRange(emp1, emp2);

        var tech = new TTecnologiaTSI { Id = 10, NombreCorporativo = "DAM - Imperva" };
        ctx.Technologies.Add(tech);

        ctx.AdoptionProcessCompanies.AddRange(
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 101, IdProcesoAdopcionTSI = 776, IdEmpresaSubsidiaria = 1, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 102, IdProcesoAdopcionTSI = 776, IdEmpresaSubsidiaria = 2, Aplica = true }
        );

        var impl1 = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 201,
            IdEmpresaSubsidiaria = 1,
            IdTecnologiaTSI = 10,
            IdBuildingBlock = 2,
            IdProcesoAdopcionEmpresa = 101,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.Add(impl1);

        var driverDspm = new TDriver
        {
            IdDriver = 501,
            IdTecnologiaTSIimplementadaSubsidiaria = 201,
            DescripcionDriver = "Datastores Cloud y Repositorios",
            UnidadMedida = "Datastores",
            Cantidad = 50m,
            PrecioUnitario = 200m,
            Moneda = "USD"
        };
        ctx.Drivers.Add(driverDspm);

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var report = await service.GetEvaluationReportsAsync(776);

        Assert.NotNull(report);
        Assert.False(report.EsWaap);
        Assert.Equal("DSPM", report.BuildingBlockNombre);

        // Validar que NO contiene valores sintéticos de WAAP (Throughput, FQDNs, Req WAF deben ser 0)
        Assert.Equal(0m, report.VolumetriaReport.TotalThroughputMensualGbps);
        Assert.Equal(0, report.VolumetriaReport.TotalAppsFqdn);
        Assert.Equal(0m, report.VolumetriaReport.TotalMillonesRequestWaf);

        // Validar que los drivers son los de DSPM
        Assert.NotNull(report.VolumetriaReport.DriversGenerales);
        Assert.Equal(1, report.VolumetriaReport.TotalCantidadDrivers);
        Assert.Equal(10000m, report.VolumetriaReport.TotalInversionDrivers);

        var bcpDriver = report.VolumetriaReport.DriversGenerales.FirstOrDefault(d => d.EmpresaId == 1 && d.DriverId > 0);
        Assert.NotNull(bcpDriver);
        Assert.Equal("Datastores Cloud y Repositorios", bcpDriver.DescripcionDriver);
        Assert.Equal("Datastores", bcpDriver.UnidadMedida);
        Assert.Equal(50m, bcpDriver.Cantidad);
        Assert.Equal(200m, bcpDriver.PrecioUnitario);
        Assert.Equal(10000m, bcpDriver.CostoTotal);

        // Validar que Mi Banco (sin drivers configurados) tiene la fila descriptiva
        var miBancoRow = report.VolumetriaReport.DriversGenerales.FirstOrDefault(d => d.EmpresaId == 2);
        Assert.NotNull(miBancoRow);
        Assert.Equal(0, miBancoRow.DriverId);
        Assert.Equal("Sin drivers de consumo registrados", miBancoRow.DescripcionDriver);

        // Validar Matriz de Drivers en Columnas (Pestaña 3)
        Assert.NotNull(report.VolumetriaReport.MatrizDrivers);
        Assert.Single(report.VolumetriaReport.MatrizDrivers.ColumnasDrivers);
        Assert.Equal("Datastores Cloud y Repositorios / Datastores", report.VolumetriaReport.MatrizDrivers.ColumnasDrivers[0]);

        var bcpMatrixRow = report.VolumetriaReport.MatrizDrivers.Filas.FirstOrDefault(f => f.EmpresaId == 1);
        Assert.NotNull(bcpMatrixRow);
        Assert.Equal(50m, bcpMatrixRow.ValoresPorColumnaDriver["Datastores Cloud y Repositorios / Datastores"]);

        var miBancoMatrixRow = report.VolumetriaReport.MatrizDrivers.Filas.FirstOrDefault(f => f.EmpresaId == 2);
        Assert.NotNull(miBancoMatrixRow);
        Assert.Equal(0m, miBancoMatrixRow.ValoresPorColumnaDriver["Datastores Cloud y Repositorios / Datastores"]);

        // Validar Reporte 5 (Proyección de Costos AS-IS)
        Assert.NotNull(report.CostosAsIsReport);
        Assert.Equal(10000m, report.CostosAsIsReport.TotalInversionGeneral);
        Assert.Equal(1, report.CostosAsIsReport.TotalItemsConCosto);
    }

    [Fact]
    public async Task BuildProcessDetailAsync_IsolatesTechnologiesByProcesoEmpresa_WithoutDuplicates()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var bb = new TBuildingBlock { Id = 1, Nombre = "WAAP" };
        ctx.BuildingBlocks.Add(bb);

        var emp = new TEmpresaSubsidiaria { Id = 10, Nombre = "Mibanco" };
        ctx.Companies.Add(emp);

        var tech1 = new TTecnologiaTSI { Id = 50, NombreCorporativo = "Imperva" };
        var tech2 = new TTecnologiaTSI { Id = 51, NombreCorporativo = "Akamai" };
        ctx.Technologies.AddRange(tech1, tech2);

        var proceso120 = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 120,
            CodigoProceso = "PROC-120",
            NombreProceso = "Evaluación WAAP 3",
            IdBuildingBlock = 1,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso120);

        var pe120 = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 301,
            IdProcesoAdopcionTSI = 120,
            IdEmpresaSubsidiaria = 10,
            Aplica = true
        };
        ctx.AdoptionProcessCompanies.Add(pe120);

        var pe99 = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 201,
            IdProcesoAdopcionTSI = 99,
            IdEmpresaSubsidiaria = 10,
            Aplica = true
        };
        ctx.AdoptionProcessCompanies.Add(pe99);

        var oldImpl = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 1001,
            IdEmpresaSubsidiaria = 10,
            IdTecnologiaTSI = 50,
            IdBuildingBlock = 1,
            IdProcesoAdopcionEmpresa = 201
        };

        var currentImpl = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 1002,
            IdEmpresaSubsidiaria = 10,
            IdTecnologiaTSI = 51,
            IdBuildingBlock = 1,
            IdProcesoAdopcionEmpresa = 301,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.AddRange(oldImpl, currentImpl);

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var detail = await service.GetProcessDetailAsync(120);

        Assert.NotNull(detail);
        Assert.Single(detail.EmpresasParticipantes);
        var mibanco = detail.EmpresasParticipantes[0];
        Assert.Single(mibanco.TecnologiasImplementadas);
        Assert.Equal("Akamai", mibanco.TecnologiasImplementadas[0].TecnologiaNombre);
        Assert.Equal(1002, mibanco.TecnologiasImplementadas[0].Id);
    }

    [Fact]
    public async Task SaveContractAsync_StoresAndReturnsMontoAnualAndTrianual()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var impl = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 50,
            IdEmpresaSubsidiaria = 1,
            IdTecnologiaTSI = 1,
            IdBuildingBlock = 1
        };
        ctx.ImplementedTechnologies.Add(impl);
        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var command = new SaveContractCommand(
            TecnologiaImplementadaId: 50,
            NumeroContrato: "CTR-2026-001",
            EsAdenda: false,
            ContratoPadreId: null,
            FechaInicio: new DateTime(2026, 1, 1),
            FechaFin: new DateTime(2028, 12, 31),
            FechaAdjudicacion: null,
            RutaDocumento: null,
            Monto: 150000m,
            Moneda: "USD",
            Observaciones: "Vendor: AKAMAI / Partner: Apukay",
            ActorUserId: Guid.NewGuid(),
            CorrelationId: "corr-test-amounts",
            EsPayg: false,
            MontoAnual: 50000m,
            MontoTrianual: 150000m);

        var result = await service.SaveContractAsync(command);
        Assert.True(result.Succeeded);

        var contract = await ctx.TechnologyContracts.FirstOrDefaultAsync(c => c.IdTecnologiaTSIimplementadaSubsidiaria == 50);
        Assert.NotNull(contract);
        Assert.Equal(150000m, contract.MontoContratado);
        Assert.Equal(50000m, contract.MontoAnual);
        Assert.Equal(150000m, contract.MontoTrianual);
        Assert.Equal("Vendor: AKAMAI / Partner: Apukay", contract.Observaciones);
    }

    [Fact]
    public async Task UpdateCompanyCapabilityStateAsync_UpdatesAndPersistsState()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 1,
            CodigoProceso = "PROC-CAP-001",
            NombreProceso = "Proceso WAAP",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1
        };
        var procEmp = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 100,
            IdProcesoAdopcionTSI = 1,
            IdEmpresaSubsidiaria = 5,
            Aplica = true
        };
        ctx.AdoptionProcesses.Add(proceso);
        ctx.AdoptionProcessCompanies.Add(procEmp);
        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());

        var result = await service.UpdateCompanyCapabilityStateAsync(
            1, 5, 20, "A", "Activo en producción", Guid.NewGuid(), "corr-1");

        Assert.True(result.Succeeded);

        var saved = await ctx.ProcessCompanyCapabilities
            .FirstOrDefaultAsync(c => c.IdProcesoAdopcionEmpresa == 100 && c.IdCapacidad == 20);

        Assert.NotNull(saved);
        Assert.Equal("A", saved.EstadoCobertura);
        Assert.Equal("Activo en producción", saved.Comentario);
    }

    [Fact]
    public async Task UpdateCompanyDriverVolumeAsync_UpdatesAndPersistsDriver()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 2,
            CodigoProceso = "PROC-VOL-001",
            NombreProceso = "Proceso WAAP Volumetría",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1
        };
        var procEmp = new TProcesoAdopcionEmpresa
        {
            IdProcesoAdopcionEmpresa = 200,
            IdProcesoAdopcionTSI = 2,
            IdEmpresaSubsidiaria = 8,
            Aplica = true
        };
        var impl = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 88,
            IdEmpresaSubsidiaria = 8,
            IdBuildingBlock = 10,
            IdProcesoAdopcionEmpresa = 200,
            EsTecnologiaPrimaria = true
        };
        ctx.AdoptionProcesses.Add(proceso);
        ctx.AdoptionProcessCompanies.Add(procEmp);
        ctx.ImplementedTechnologies.Add(impl);
        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());

        var res = await service.UpdateCompanyDriverVolumeAsync(
            2, 8, "throughput", 1450.5m, Guid.NewGuid(), "corr-2");

        Assert.True(res.Succeeded);

        var drv = await ctx.Drivers.FirstOrDefaultAsync(d => d.IdTecnologiaTSIimplementadaSubsidiaria == 88);
        Assert.NotNull(drv);
        Assert.Equal("Throughput (mensual)", drv.DescripcionDriver);
        Assert.Equal(1450.5m, drv.Cantidad);
    }

    [Fact]
    public async Task SaveVencimientoReportDriversAsync_PersistsJsonConfiguration()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 5,
            CodigoProceso = "PROC-DRV-005",
            NombreProceso = "Proceso Drivers Test",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);
        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());

        var selected = new[] { "Throughput (mensual)", "Millones Request WAF (mensual)" };
        var res = await service.SaveVencimientoReportDriversAsync(5, selected, Guid.NewGuid(), "corr-5");

        Assert.True(res.Succeeded);

        var updated = await ctx.AdoptionProcesses.FirstAsync(p => p.IdProcesoAdopcionTSI == 5);
        Assert.NotNull(updated.DriversReporteVencimiento);
        Assert.Contains("Throughput (mensual)", updated.DriversReporteVencimiento);
        Assert.Contains("Millones Request WAF (mensual)", updated.DriversReporteVencimiento);
    }

    [Fact]
    public async Task GetEvaluationReportsAsync_GeneratesDynamicMilestonesFromContractDates_GroupedByYearMonth_AndCalculatesContractAmounts()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 99,
            CodigoProceso = "PROC-DYN-099",
            NombreProceso = "Evaluación WAAP Dinámica",
            IdBuildingBlock = 1,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var bb = new TBuildingBlock { Id = 1, Nombre = "Web Application Firewall" };
        ctx.BuildingBlocks.Add(bb);

        var emp1 = new TEmpresaSubsidiaria { Id = 101, Nombre = "Yape Bolivia", Pais = "Bolivia" };
        var emp2 = new TEmpresaSubsidiaria { Id = 102, Nombre = "Banco de Crédito de Bolivia", Pais = "Bolivia" };
        var emp3 = new TEmpresaSubsidiaria { Id = 103, Nombre = "Credicorp Capital", Pais = "Perú" };
        ctx.Companies.AddRange(emp1, emp2, emp3);

        var tech1 = new TTecnologiaTSI { Id = 201, NombreCorporativo = "Cloudflare WAAP" };
        var tech2 = new TTecnologiaTSI { Id = 202, NombreCorporativo = "Imperva WAF" };
        var tech3 = new TTecnologiaTSI { Id = 203, NombreCorporativo = "AWS WAF (PAYG)" };
        ctx.Technologies.AddRange(tech1, tech2, tech3);

        ctx.AdoptionProcessCompanies.AddRange(
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 301, IdProcesoAdopcionTSI = 99, IdEmpresaSubsidiaria = 101, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 302, IdProcesoAdopcionTSI = 99, IdEmpresaSubsidiaria = 102, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 303, IdProcesoAdopcionTSI = 99, IdEmpresaSubsidiaria = 103, Aplica = true }
        );

        var impl1 = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 401,
            IdEmpresaSubsidiaria = 101,
            IdTecnologiaTSI = 201,
            IdBuildingBlock = 1,
            IdProcesoAdopcionEmpresa = 301,
            EsTecnologiaPrimaria = true
        };
        var impl2 = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 402,
            IdEmpresaSubsidiaria = 102,
            IdTecnologiaTSI = 202,
            IdBuildingBlock = 1,
            IdProcesoAdopcionEmpresa = 302,
            EsTecnologiaPrimaria = true
        };
        var impl3 = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 403,
            IdEmpresaSubsidiaria = 103,
            IdTecnologiaTSI = 203,
            IdBuildingBlock = 1,
            IdProcesoAdopcionEmpresa = 303,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.AddRange(impl1, impl2, impl3);

        // Yape Bolivia: contract expires 2027-05-22, amount 31,642.19 USD
        ctx.TechnologyContracts.Add(new TContratoTecnologia
        {
            IdContratoTecnologia = 501,
            IdTecnologiaTSIimplementadaSubsidiaria = 401,
            NumeroContrato = "CT-YB",
            FechaFin = new DateTime(2027, 5, 22),
            MontoContratado = 31642.19m,
            Moneda = "USD",
            EsPayg = false
        });

        // Banco de Crédito Bolivia: contract expires 2027-09-29, amount 40,963.68 USD
        ctx.TechnologyContracts.Add(new TContratoTecnologia
        {
            IdContratoTecnologia = 502,
            IdTecnologiaTSIimplementadaSubsidiaria = 402,
            NumeroContrato = "CT-BCB",
            FechaFin = new DateTime(2027, 9, 29),
            MontoContratado = 40963.68m,
            Moneda = "USD",
            EsPayg = false
        });

        // Credicorp Capital: PAYG (no expiration date)
        ctx.TechnologyContracts.Add(new TContratoTecnologia
        {
            IdContratoTecnologia = 503,
            IdTecnologiaTSIimplementadaSubsidiaria = 403,
            NumeroContrato = "CT-PAYG",
            FechaFin = null,
            MontoContratado = 0m,
            Moneda = "USD",
            EsPayg = true
        });

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var report = await service.GetEvaluationReportsAsync(99);

        Assert.NotNull(report);
        var venc = report.VencimientoContratosReport;
        Assert.NotNull(venc);

        // Dynamic milestones must be: May-27, Set-27, PAYG
        Assert.Equal(3, venc.PeriodosHito.Count);
        Assert.Equal("May-27", venc.PeriodosHito[0].PeriodoLabel);
        Assert.Equal("Set-27", venc.PeriodosHito[1].PeriodoLabel);
        Assert.Equal("PAYG", venc.PeriodosHito[2].PeriodoLabel);

        // Check rows
        var rowYb = venc.Filas.FirstOrDefault(f => f.EmpresaNombre == "Yape Bolivia");
        Assert.NotNull(rowYb);
        Assert.Equal(31642.19m, rowYb.MontoContrato);
        Assert.True(rowYb.HitoExpiracionPorPeriodo.TryGetValue("May-27", out var activeYb) && activeYb);
        Assert.False(rowYb.HitoExpiracionPorPeriodo.TryGetValue("Set-27", out var activeYbSet) && activeYbSet);

        var rowBcb = venc.Filas.FirstOrDefault(f => f.EmpresaNombre == "Banco de Crédito de Bolivia");
        Assert.NotNull(rowBcb);
        Assert.Equal(40963.68m, rowBcb.MontoContrato);
        Assert.True(rowBcb.HitoExpiracionPorPeriodo.TryGetValue("Set-27", out var activeBcb) && activeBcb);

        var rowCc = venc.Filas.FirstOrDefault(f => f.EmpresaNombre == "Credicorp Capital");
        Assert.NotNull(rowCc);
        Assert.True(rowCc.HitoExpiracionPorPeriodo.TryGetValue("PAYG", out var activeCc) && activeCc);
    }

    [Fact]
    public void EvaluationReportFormatHelper_FormatsDecimalsAndIntegersCorrectly()
    {
        // Whole numbers / zero decimals -> integer format without trailing decimals
        Assert.Equal("0", EvaluationReportFormatHelper.FormatSmart(0m));
        Assert.Equal("120", EvaluationReportFormatHelper.FormatSmart(120m));
        Assert.Equal("120", EvaluationReportFormatHelper.FormatSmart(120.00m));
        Assert.Equal("3,767", EvaluationReportFormatHelper.FormatSmart(3767m));

        // Decimals -> preserves up to at least 3 decimal places without rounding to 1 decimal
        Assert.Equal("0.06", EvaluationReportFormatHelper.FormatSmart(0.06m));
        Assert.Equal("0.005", EvaluationReportFormatHelper.FormatSmart(0.005m));
        Assert.Equal("0.01", EvaluationReportFormatHelper.FormatSmart(0.01m));
        Assert.Equal("0.15", EvaluationReportFormatHelper.FormatSmart(0.15m));
        Assert.Equal("0.1", EvaluationReportFormatHelper.FormatSmart(0.1m));
        Assert.Equal("1,370.5", EvaluationReportFormatHelper.FormatSmart(1370.5m));
        Assert.Equal("0.0005", EvaluationReportFormatHelper.FormatSmart(0.0005m));

        // Nullable overload
        Assert.Equal("—", EvaluationReportFormatHelper.FormatSmart((decimal?)null));
        Assert.Equal("—", EvaluationReportFormatHelper.FormatSmart((decimal?)0m));
        Assert.Equal("0.06", EvaluationReportFormatHelper.FormatSmart((decimal?)0.06m));
        Assert.Equal("120", EvaluationReportFormatHelper.FormatSmart((decimal?)120m));
    }

    [Fact]
    public async Task UpdateCompanyOrderAsync_SavesOrderedCompanyIdsAndAuditTrail()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 910,
            CodigoProceso = "EVAL-ORDEN-001",
            NombreProceso = "Proceso Test Orden",
            IdBuildingBlock = 1,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var c1 = new TEmpresaSubsidiaria { Id = 1, Nombre = "Empresa Alfa" };
        var c2 = new TEmpresaSubsidiaria { Id = 2, Nombre = "Empresa Beta" };
        var c3 = new TEmpresaSubsidiaria { Id = 3, Nombre = "Empresa Gamma" };
        ctx.Companies.AddRange(c1, c2, c3);

        ctx.AdoptionProcessCompanies.AddRange(
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 11, IdProcesoAdopcionTSI = 910, IdEmpresaSubsidiaria = 1, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 12, IdProcesoAdopcionTSI = 910, IdEmpresaSubsidiaria = 2, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 13, IdProcesoAdopcionTSI = 910, IdEmpresaSubsidiaria = 3, Aplica = true }
        );
        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var updateResult = await service.UpdateCompanyOrderAsync(910, [3, 1, 2], Guid.NewGuid(), "test-corr");

        Assert.True(updateResult.Succeeded);
        var updatedProc = await ctx.AdoptionProcesses.FindAsync(910);
        Assert.NotNull(updatedProc);
        Assert.Equal("[3,1,2]", updatedProc.OrdenEmpresas);
    }

    [Fact]
    public async Task GetProcessDetailAndReports_RespectsConfiguredCompanyVisualOrder()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var ctx = new CatalogDbContext(options);

        var bb = new TBuildingBlock { Id = 10, Nombre = "WAAP Corporativo" };
        ctx.BuildingBlocks.Add(bb);

        var c1 = new TEmpresaSubsidiaria { Id = 1, Nombre = "BCP Perú" };
        var c2 = new TEmpresaSubsidiaria { Id = 2, Nombre = "BCP Bolivia" };
        var c3 = new TEmpresaSubsidiaria { Id = 3, Nombre = "Mibanco" };
        ctx.Companies.AddRange(c1, c2, c3);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 920,
            CodigoProceso = "EVAL-ORDEN-002",
            NombreProceso = "Evaluación WAAP",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1,
            OrdenEmpresas = "[3, 2, 1]" // Visual sequence: Mibanco (3), BCP Bolivia (2), BCP Perú (1)
        };
        ctx.AdoptionProcesses.Add(proceso);

        ctx.AdoptionProcessCompanies.AddRange(
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 21, IdProcesoAdopcionTSI = 920, IdEmpresaSubsidiaria = 1, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 22, IdProcesoAdopcionTSI = 920, IdEmpresaSubsidiaria = 2, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 23, IdProcesoAdopcionTSI = 920, IdEmpresaSubsidiaria = 3, Aplica = true }
        );
        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());

        // 1. Validar orden en Details (Convocatoria)
        var detail = await service.GetProcessDetailAsync(920);
        Assert.NotNull(detail);
        Assert.Equal("[3, 2, 1]", detail.OrdenEmpresas);
        Assert.Equal(3, detail.EmpresasParticipantes.Count);
        Assert.Equal(3, detail.EmpresasParticipantes[0].EmpresaId); // Mibanco first
        Assert.Equal(2, detail.EmpresasParticipantes[1].EmpresaId); // BCP Bolivia second
        Assert.Equal(1, detail.EmpresasParticipantes[2].EmpresaId); // BCP Perú third

        // 2. Validar orden en Reports (Reportería y Seguimiento)
        var reports = await service.GetEvaluationReportsAsync(920);
        Assert.NotNull(reports);
        Assert.Equal("[3, 2, 1]", reports.OrdenEmpresas);

        // Reporte 1: Alcance
        Assert.Equal(3, reports.AlcanceReport.Count);
        Assert.Equal(3, reports.AlcanceReport[0].EmpresaId);
        Assert.Equal(2, reports.AlcanceReport[1].EmpresaId);
        Assert.Equal(1, reports.AlcanceReport[2].EmpresaId);

        // Reporte 3: Volumetría
        Assert.Equal(3, reports.VolumetriaReport.Filas.Count);
        Assert.Equal(3, reports.VolumetriaReport.Filas[0].EmpresaId);
        Assert.Equal(2, reports.VolumetriaReport.Filas[1].EmpresaId);
        Assert.Equal(1, reports.VolumetriaReport.Filas[2].EmpresaId);

        // Reporte 4: Capacidades
        Assert.Equal(3, reports.CapacidadesMatrixReport.Filas.Count);
        Assert.Equal(3, reports.CapacidadesMatrixReport.Filas[0].EmpresaId);
        Assert.Equal(2, reports.CapacidadesMatrixReport.Filas[1].EmpresaId);
        Assert.Equal(1, reports.CapacidadesMatrixReport.Filas[2].EmpresaId);
    }

    [Fact]
    public async Task GetEvaluationReportsAsync_WhenProcessDriversExist_OnlyReturnsProcessDriversInAvailableDrivers_IgnoringLegacyTDriverDuplicates()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new CatalogDbContext(options);

        var bb = new TBuildingBlock { Id = 10, Nombre = "ETP + Awareness" };
        ctx.BuildingBlocks.Add(bb);

        var empresa1 = new TEmpresaSubsidiaria { Id = 1, Nombre = "Empresa A" };
        var empresa2 = new TEmpresaSubsidiaria { Id = 2, Nombre = "Empresa B" };
        ctx.Companies.AddRange(empresa1, empresa2);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 801,
            CodigoProceso = "PROC-801",
            NombreProceso = "Evaluación ETP Proofpoint",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var pae1 = new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 101, IdProcesoAdopcionTSI = 801, IdEmpresaSubsidiaria = 1, Aplica = true };
        var pae2 = new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 102, IdProcesoAdopcionTSI = 801, IdEmpresaSubsidiaria = 2, Aplica = true };
        ctx.AdoptionProcessCompanies.AddRange(pae1, pae2);

        // 2 drivers oficiales a nivel de proceso
        var dp1 = new TDriverProceso { IdDriverProceso = 1, IdProcesoAdopcionTSI = 801, DescripcionDriver = "Licencias 2026", UnidadMedida = "Unidades", OrdenVisual = 1 };
        var dp2 = new TDriverProceso { IdDriverProceso = 2, IdProcesoAdopcionTSI = 801, DescripcionDriver = "Licencias 2027", UnidadMedida = "Unidades", OrdenVisual = 2 };
        ctx.ProcessDrivers.AddRange(dp1, dp2);

        // Valores por empresa
        ctx.CompanyDrivers.AddRange(
            new TDriverEmpresa { IdDriverEmpresa = 1, IdProcesoAdopcionEmpresa = 101, IdDriverProceso = 1, Cantidad = 100, PrecioUnitario = 10, CostoTotal = 1000 },
            new TDriverEmpresa { IdDriverEmpresa = 2, IdProcesoAdopcionEmpresa = 101, IdDriverProceso = 2, Cantidad = 120, PrecioUnitario = 10, CostoTotal = 1200 },
            new TDriverEmpresa { IdDriverEmpresa = 3, IdProcesoAdopcionEmpresa = 102, IdDriverProceso = 1, Cantidad = 50, PrecioUnitario = 10, CostoTotal = 500 },
            new TDriverEmpresa { IdDriverEmpresa = 4, IdProcesoAdopcionEmpresa = 102, IdDriverProceso = 2, Cantidad = 60, PrecioUnitario = 10, CostoTotal = 600 }
        );

        // Tecnología implementada con drivers huérfanos/antiguos en la tabla legacy TDriver
        var tech = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 50,
            IdEmpresaSubsidiaria = 1,
            IdBuildingBlock = 10,
            IdProcesoAdopcionEmpresa = 101,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.Add(tech);

        ctx.Drivers.AddRange(
            new TDriver { IdDriver = 901, IdTecnologiaTSIimplementadaSubsidiaria = 50, DescripcionDriver = "Licencias 2026", UnidadMedida = "Unidades", Cantidad = 100 },
            new TDriver { IdDriver = 902, IdTecnologiaTSIimplementadaSubsidiaria = 50, DescripcionDriver = "Licencias 2026 (antiguo duplicado)", UnidadMedida = "unidad", Cantidad = 90 },
            new TDriver { IdDriver = 903, IdTecnologiaTSIimplementadaSubsidiaria = 50, DescripcionDriver = "Driver borrado 2025", UnidadMedida = "Unidades", Cantidad = 10 }
        );

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var reports = await service.GetEvaluationReportsAsync(801);

        Assert.NotNull(reports);
        var availableDrivers = reports.VencimientoContratosReport.TodosLosDriversDisponibles;
        Assert.NotNull(availableDrivers);

        // Únicamente deben figurar los 2 drivers definidos en TDriverProceso
        Assert.Equal(2, availableDrivers.Count);
        Assert.Contains("Licencias 2026", availableDrivers);
        Assert.Contains("Licencias 2027", availableDrivers);
        Assert.DoesNotContain("Licencias 2026 (antiguo duplicado)", availableDrivers);
        Assert.DoesNotContain("Driver borrado 2025", availableDrivers);

        // Verificar que los valores por empresa provienen de TDriverEmpresa
        var rowEmpA = reports.VencimientoContratosReport.Filas.First(f => f.EmpresaId == 1);
        Assert.Equal(100m, rowEmpA.DriversPorNombre!["Licencias 2026"]);
        Assert.Equal(120m, rowEmpA.DriversPorNombre!["Licencias 2027"]);
    }

    [Fact]
    public async Task DeleteProcessDriverAsync_RemovesDriver_CleansVencimientoSelection_AndRemovesLegacyDuplicates()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new CatalogDbContext(options);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 802,
            CodigoProceso = "PROC-802",
            NombreProceso = "Evaluación Test Delete",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1,
            DriversReporteVencimiento = "[\"Driver A\",\"Driver B\"]"
        };
        ctx.AdoptionProcesses.Add(proceso);

        var pae = new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 201, IdProcesoAdopcionTSI = 802, IdEmpresaSubsidiaria = 1, Aplica = true };
        ctx.AdoptionProcessCompanies.Add(pae);

        var dpA = new TDriverProceso { IdDriverProceso = 10, IdProcesoAdopcionTSI = 802, DescripcionDriver = "Driver A", UnidadMedida = "Unidades" };
        var dpB = new TDriverProceso { IdDriverProceso = 11, IdProcesoAdopcionTSI = 802, DescripcionDriver = "Driver B", UnidadMedida = "Unidades" };
        ctx.ProcessDrivers.AddRange(dpA, dpB);

        ctx.CompanyDrivers.AddRange(
            new TDriverEmpresa { IdDriverEmpresa = 10, IdProcesoAdopcionEmpresa = 201, IdDriverProceso = 10, Cantidad = 100 },
            new TDriverEmpresa { IdDriverEmpresa = 11, IdProcesoAdopcionEmpresa = 201, IdDriverProceso = 11, Cantidad = 200 }
        );

        var tech = new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 60,
            IdEmpresaSubsidiaria = 1,
            IdBuildingBlock = 10,
            IdProcesoAdopcionEmpresa = 201,
            EsTecnologiaPrimaria = true
        };
        ctx.ImplementedTechnologies.Add(tech);

        ctx.Drivers.AddRange(
            new TDriver { IdDriver = 910, IdTecnologiaTSIimplementadaSubsidiaria = 60, DescripcionDriver = "Driver A", UnidadMedida = "Unidades" },
            new TDriver { IdDriver = 911, IdTecnologiaTSIimplementadaSubsidiaria = 60, DescripcionDriver = "Driver A / unidad", UnidadMedida = "Unidades" }
        );

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var result = await service.DeleteProcessDriverAsync(802, 10, Guid.NewGuid(), "test-corr");

        Assert.True(result.Succeeded);

        // Driver A ya no debe existir en ProcessDrivers ni en CompanyDrivers
        Assert.Null(await ctx.ProcessDrivers.FirstOrDefaultAsync(d => d.IdDriverProceso == 10));
        Assert.Empty(await ctx.CompanyDrivers.Where(d => d.IdDriverProceso == 10).ToListAsync());

        // Se debe haber limpiado de DriversReporteVencimiento
        var updatedProc = await ctx.AdoptionProcesses.FirstAsync(p => p.IdProcesoAdopcionTSI == 802);
        Assert.Equal("[\"Driver B\"]", updatedProc.DriversReporteVencimiento);

        // Se deben haber eliminado los registros homólogos de la tabla legacy TDriver
        var legacyA = await ctx.Drivers.Where(d => d.IdTecnologiaTSIimplementadaSubsidiaria == 60 && d.DescripcionDriver!.StartsWith("Driver A")).ToListAsync();
        Assert.Empty(legacyA);
    }

    [Fact]
    public async Task GetEvaluationReportsAsync_CompanyWithoutTechnology_ReportsSinTecnologiaRegistrada()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new CatalogDbContext(options);

        // Registrar tecnología por defecto en el catálogo (ej. Imperva con Id = 1)
        var defaultTech = new TTecnologiaTSI { Id = 1, NombreCorporativo = "Imperva WAF" };
        var bcpTech = new TTecnologiaTSI { Id = 1547, NombreCorporativo = "ETP + Awareness: Proofpoint" };
        ctx.Technologies.AddRange(defaultTech, bcpTech);

        var empresaBcp = new TEmpresaSubsidiaria { Id = 1, Nombre = "Banco de Crédito del Perú" };
        var empresaMibanco = new TEmpresaSubsidiaria { Id = 2, Nombre = "Mibanco" };
        ctx.Companies.AddRange(empresaBcp, empresaMibanco);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 803,
            CodigoProceso = "PROC-803",
            NombreProceso = "Evaluación ETP Proofpoint",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var pae1 = new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 301, IdProcesoAdopcionTSI = 803, IdEmpresaSubsidiaria = 1, Aplica = true };
        var pae2 = new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 302, IdProcesoAdopcionTSI = 803, IdEmpresaSubsidiaria = 2, Aplica = true };
        ctx.AdoptionProcessCompanies.AddRange(pae1, pae2);

        // Solo BCP tiene tecnología registrada
        ctx.ImplementedTechnologies.Add(new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 199,
            IdEmpresaSubsidiaria = 1,
            IdTecnologiaTSI = 1547,
            IdBuildingBlock = 10,
            IdProcesoAdopcionEmpresa = 301,
            EsTecnologiaPrimaria = true
        });

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var reports = await service.GetEvaluationReportsAsync(803);

        Assert.NotNull(reports);

        // BCP debe mostrar su tecnología registrada
        var bcpAlcance = reports.AlcanceReport.First(r => r.EmpresaId == 1);
        Assert.Equal("ETP + Awareness: Proofpoint", bcpAlcance.TecnologiaAsIs);

        // Mibanco no tiene tecnología registrada -> Debe mostrar "Sin tecnología registrada" y no Imperva
        var mibancoAlcance = reports.AlcanceReport.First(r => r.EmpresaId == 2);
        Assert.Equal("Sin tecnología registrada", mibancoAlcance.TecnologiaAsIs);
        Assert.Equal("—", mibancoAlcance.TipoContrato);
        Assert.Equal("—", mibancoAlcance.Vendor);
        Assert.Equal("—", mibancoAlcance.Partner);
        Assert.Equal("—", mibancoAlcance.TipoOperacion);

        var mibancoVenc = reports.VencimientoContratosReport.Filas.First(r => r.EmpresaId == 2);
        Assert.Equal("Sin tecnología registrada", mibancoVenc.ProductoActual);

        var mibancoCap = reports.CapacidadesMatrixReport.Filas.First(r => r.EmpresaId == 2);
        Assert.Equal("Sin tecnología registrada", mibancoCap.TecnologiaAsIs);
    }

    [Fact]
    public async Task UpdateCompanyDriverVolumeAsync_CompanyWithoutTechnology_DoesNotInjectFakeTechnology()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new CatalogDbContext(options);

        // En la BD existe Imperva con Id = 1
        ctx.Technologies.Add(new TTecnologiaTSI { Id = 1, NombreCorporativo = "Imperva WAF" });

        var empresa = new TEmpresaSubsidiaria { Id = 2, Nombre = "Mibanco" };
        ctx.Companies.Add(empresa);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 804,
            CodigoProceso = "PROC-804",
            NombreProceso = "Evaluación Sin Tecnología",
            IdBuildingBlock = 10,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var pae = new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 302, IdProcesoAdopcionTSI = 804, IdEmpresaSubsidiaria = 2, Aplica = true };
        ctx.AdoptionProcessCompanies.Add(pae);

        var dp = new TDriverProceso { IdDriverProceso = 5, IdProcesoAdopcionTSI = 804, DescripcionDriver = "Usuarios Activos", UnidadMedida = "Usuarios" };
        ctx.ProcessDrivers.Add(dp);

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());

        // Actualizar volumen de driver para Mibanco (empresaId = 2)
        var result = await service.UpdateCompanyDriverVolumeAsync(804, 2, "Usuarios Activos", 500m, Guid.NewGuid(), "corr-vol");

        Assert.True(result.Succeeded);

        // NUNCA debe haberse creado una fila en TTecnologiaTSIimplementadaSubsidiaria con Imperva
        var implTechCount = await ctx.ImplementedTechnologies.CountAsync(it => it.IdEmpresaSubsidiaria == 2);
        Assert.Equal(0, implTechCount);

        // El valor debe haberse registrado en TDriverEmpresa
        var companyDriver = await ctx.CompanyDrivers.FirstOrDefaultAsync(cd => cd.IdProcesoAdopcionEmpresa == 302 && cd.IdDriverProceso == 5);
        Assert.NotNull(companyDriver);
        Assert.Equal(500m, companyDriver.Cantidad);
    }

    [Fact]
    public async Task GetEvaluationReportsAsync_WhenWaapProcessDriversHaveNoValues_DefaultsToZeroAndNeverGeneratesFakeDemoValues()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        await using var ctx = new CatalogDbContext(options);

        var bb = new TBuildingBlock { Id = 1, Nombre = "WAAP" };
        ctx.BuildingBlocks.Add(bb);

        var empresa = new TEmpresaSubsidiaria { Id = 1, Nombre = "Banco de Crédito de Perú", Pais = "Perú" };
        ctx.Companies.Add(empresa);

        var proceso = new TProcesoAdopcionTSI
        {
            IdProcesoAdopcionTSI = 289,
            CodigoProceso = "EVAL-TSI-20260925-411",
            NombreProceso = "Evaluacion WAF - BCP",
            IdBuildingBlock = 1,
            IdEstadoAdopcionTSI = 1
        };
        ctx.AdoptionProcesses.Add(proceso);

        var pae = new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 501, IdProcesoAdopcionTSI = 289, IdEmpresaSubsidiaria = 1, Aplica = true };
        ctx.AdoptionProcessCompanies.Add(pae);

        // Se configuran drivers formales en el proceso pero con Cantidad = 0 o sin registros aún en TDriverEmpresa
        ctx.ProcessDrivers.AddRange(
            new TDriverProceso { IdDriverProceso = 1, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Throughput", UnidadMedida = "Gbps / mensual", OrdenVisual = 1 },
            new TDriverProceso { IdDriverProceso = 2, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Ancho de Banda", UnidadMedida = "Gbps/mensual", OrdenVisual = 2 },
            new TDriverProceso { IdDriverProceso = 3, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Dominio", UnidadMedida = "unidad", OrdenVisual = 3 },
            new TDriverProceso { IdDriverProceso = 4, IdProcesoAdopcionTSI = 289, DescripcionDriver = "subdominio", UnidadMedida = "unidad", OrdenVisual = 4 },
            new TDriverProceso { IdDriverProceso = 5, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Cantidad de aplicaciones (FQDN)", UnidadMedida = "unidad", OrdenVisual = 5 },
            new TDriverProceso { IdDriverProceso = 6, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Cantidad de FQDN en API Security", UnidadMedida = "unidad", OrdenVisual = 6 },
            new TDriverProceso { IdDriverProceso = 7, IdProcesoAdopcionTSI = 289, DescripcionDriver = "API Protection / API Security Request", UnidadMedida = "M x Mes", OrdenVisual = 7 },
            new TDriverProceso { IdDriverProceso = 8, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Request en antibot", UnidadMedida = "(M)/mensual", OrdenVisual = 8 },
            new TDriverProceso { IdDriverProceso = 9, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Request en WAF", UnidadMedida = "M/mensual", OrdenVisual = 9 },
            new TDriverProceso { IdDriverProceso = 10, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Data Transfer", UnidadMedida = "TB/mensual", OrdenVisual = 10 },
            new TDriverProceso { IdDriverProceso = 11, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Request Size", UnidadMedida = "mensual", OrdenVisual = 11 },
            new TDriverProceso { IdDriverProceso = 12, IdProcesoAdopcionTSI = 289, DescripcionDriver = "Response Size", UnidadMedida = "mensual", OrdenVisual = 12 }
        );

        // Los valores registrados para BCP son 0.00
        for (int i = 1; i <= 12; i++)
        {
            ctx.CompanyDrivers.Add(new TDriverEmpresa
            {
                IdDriverEmpresa = i,
                IdProcesoAdopcionEmpresa = 501,
                IdDriverProceso = i,
                Cantidad = 0m,
                PrecioUnitario = 0m,
                CostoTotal = 0m,
                Moneda = "USD"
            });
        }

        await ctx.SaveChangesAsync();

        var service = new AdoptionProcessService(ctx, new NullAuditTrail());
        var report = await service.GetEvaluationReportsAsync(289);

        Assert.NotNull(report);
        Assert.True(report.EsWaap);

        // Los totales proyectados globales deben ser CERO (nunca los valores ficticios de demo 225, 22, 135)
        Assert.Equal(0m, report.VencimientoContratosReport.TotalThroughputGbMes);
        Assert.Equal(0, report.VencimientoContratosReport.TotalAppsFqdn);
        Assert.Equal(0m, report.VencimientoContratosReport.TotalRequestWafMillonesMes);

        // Volumetría por empresa (Pestaña 3)
        Assert.Single(report.VolumetriaReport.Filas);
        var bcpVol = report.VolumetriaReport.Filas[0];
        Assert.Equal(1, bcpVol.EmpresaId);
        Assert.Equal(0m, bcpVol.ThroughputMensualGbps);
        Assert.Equal(0m, bcpVol.AnchoBandaMensualGbps);
        Assert.Equal(0, bcpVol.Dominio);
        Assert.Equal(0, bcpVol.SubDominios);
        Assert.Equal(0, bcpVol.CantidadAppsFqdn);
        Assert.Equal(0, bcpVol.CantidadAppsFqdnApiSecurity);
        Assert.Equal(0m, bcpVol.ApiProtectionRequestMillonesMes);
        Assert.Equal(0m, bcpVol.MillonesRequestAntibot);
        Assert.Equal(0m, bcpVol.MillonesRequestWaf);
        Assert.Equal(0m, bcpVol.MillonesRequestAntibotWaf);
        Assert.Equal(0m, bcpVol.DataTransferTbMensual);

        // Totales consolidados de volumetría
        Assert.Equal(0m, report.VolumetriaReport.TotalThroughputMensualGbps);
        Assert.Equal(0m, report.VolumetriaReport.TotalAnchoBandaMensualGbps);
        Assert.Equal(0, report.VolumetriaReport.TotalDominio);
        Assert.Equal(0, report.VolumetriaReport.TotalSubDominios);
        Assert.Equal(0, report.VolumetriaReport.TotalAppsFqdn);
        Assert.Equal(0, report.VolumetriaReport.TotalAppsFqdnApiSecurity);
        Assert.Equal(0m, report.VolumetriaReport.TotalApiProtectionRequestMillonesMes);
        Assert.Equal(0m, report.VolumetriaReport.TotalMillonesRequestAntibot);
        Assert.Equal(0m, report.VolumetriaReport.TotalMillonesRequestWaf);
        Assert.Equal(0m, report.VolumetriaReport.TotalMillonesRequestAntibotWaf);
        Assert.Equal(0m, report.VolumetriaReport.TotalDataTransferTbMensual);
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