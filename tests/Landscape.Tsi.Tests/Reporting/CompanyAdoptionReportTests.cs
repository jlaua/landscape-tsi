using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Reporting;
using Landscape.Tsi.Domain.Adoption;
using Landscape.Tsi.Domain.Catalogs;
using Landscape.Tsi.Infrastructure.Catalogs;
using Landscape.Tsi.Infrastructure.Reporting;

using Microsoft.EntityFrameworkCore;

namespace Landscape.Tsi.Tests.Reporting;

public sealed class CompanyAdoptionReportTests
{
    [Fact]
    public async Task GetCompanyAdoptionReport_AggregatesDomainsBuildingBlocksAndAlignment()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"adoption-report-{Guid.NewGuid():N}").Options;
        using var context = new CatalogDbContext(options);

        // Seed
        context.Domains.AddRange(
            new TmDominio { Id = 1, Dominio = "Application Security" },
            new TmDominio { Id = 2, Dominio = "Cloud Security" }
        );

        context.BuildingBlocks.AddRange(
            new TBuildingBlock { Id = 10, IdDominio = 1, Nombre = "SAST" },
            new TBuildingBlock { Id = 20, IdDominio = 2, Nombre = "CSPM" }
        );

        context.Companies.AddRange(
            new TEmpresaSubsidiaria { Id = 100, Nombre = "BCP", Pais = "Perú" },
            new TEmpresaSubsidiaria { Id = 200, Nombre = "Mibanco", Pais = "Perú" }
        );

        context.Technologies.AddRange(
            new TTecnologiaTSI { Id = 1000, NombreCorporativo = "Checkmarx" },
            new TTecnologiaTSI { Id = 2000, NombreCorporativo = "SonarQube" },
            new TTecnologiaTSI { Id = 3000, NombreCorporativo = "Prisma Cloud" }
        );

        context.StandardTechnologyHistories.Add(
            new TEstandarTecnologiaHistorico
            {
                IdEstandarTecnologia = 1,
                IdBuildingBlock = 10,
                IdTecnologiaTSI = 1000,
                RolEstandar = "PRINCIPAL",
                EstadoVigencia = "ACTIVO_VIGENTE",
                FechaInicioVigencia = DateTime.UtcNow.AddMonths(-6)
            }
        );

        context.AdoptionProcesses.Add(
            new TProcesoAdopcionTSI
            {
                IdProcesoAdopcionTSI = 1,
                IdBuildingBlock = 10,
                CodigoProceso = "PROC-01",
                NombreProceso = "Adopción SAST 2026"
            }
        );

        context.AdoptionProcessCompanies.AddRange(
            new TProcesoAdopcionEmpresa
            {
                IdProcesoAdopcionEmpresa = 1,
                IdProcesoAdopcionTSI = 1,
                IdEmpresaSubsidiaria = 100,
                Aplica = true
            },
            new TProcesoAdopcionEmpresa
            {
                IdProcesoAdopcionEmpresa = 2,
                IdProcesoAdopcionTSI = 1,
                IdEmpresaSubsidiaria = 200,
                Aplica = false,
                JustificacionNoAplica = "No desarrolla software internamente"
            }
        );

        context.ImplementedTechnologies.Add(
            new TTecnologiaTSIimplementadaSubsidiaria
            {
                IdTecnologiaTSIimplementadaSubsidiaria = 1,
                IdEmpresaSubsidiaria = 100,
                IdBuildingBlock = 10,
                IdTecnologiaTSI = 1000,
                EsTecnologiaPrimaria = true,
                VersionDesplegada = "9.5",
                IdProcesoAdopcionEmpresa = 1
            }
        );

        await context.SaveChangesAsync();

        var service = new CatalogReportingService(new FakeCatalogService(), null, context);

        var report = await service.GetCompanyAdoptionReportAsync(new CompanyAdoptionReportQuery());

        // Domain Metrics
        Assert.Equal(2, report.DomainMetrics.Count);
        var appSec = Assert.Single(report.DomainMetrics, d => d.DominioId == 1);
        Assert.Equal("Application Security", appSec.DominioNombre);
        Assert.Equal(1, appSec.TotalBuildingBlocks);
        Assert.Equal(1, appSec.TotalEmpresasConAdopcion);

        var sastBb = Assert.Single(appSec.BuildingBlocksCoverage);
        Assert.Equal("SAST", sastBb.BuildingBlockNombre);
        Assert.Equal("Checkmarx", sastBb.TecnologiaEstandarCorporativa);
        Assert.Equal(2, sastBb.EmpresasConvocadas);
        Assert.Equal(1, sastBb.EmpresasConAdopcion);
        Assert.Contains("BCP", sastBb.EmpresasNombres);

        // Alignment Rows
        Assert.Equal(2, report.TotalAlignmentRows);
        var bcpRow = Assert.Single(report.AlignmentRows, r => r.EmpresaId == 100);
        Assert.Equal("ALINEADO", bcpRow.EstadoAlineacion);
        Assert.Equal("Checkmarx", bcpRow.TecnologiaLocalImplementada);
        Assert.Equal("9.5", bcpRow.VersionDesplegada);

        var mibancoRow = Assert.Single(report.AlignmentRows, r => r.EmpresaId == 200);
        Assert.Equal("NO_APLICA", mibancoRow.EstadoAlineacion);
        Assert.Equal("No desarrolla software internamente", mibancoRow.JustificacionNoAplica);

        // KPIs
        Assert.Equal(2, report.Kpis.TotalEmpresasParticipantes);
        Assert.Equal(1, report.Kpis.TotalAlineadas);
        Assert.Equal(1, report.Kpis.TotalNoAplica);
        Assert.Equal(0, report.Kpis.TotalHomologadas);
        Assert.Equal(0, report.Kpis.TotalNoAlineadas);
        Assert.Equal(0, report.Kpis.TotalPendientes);
        Assert.Equal(100.0m, report.Kpis.PorcentajeAlineacionGlobal);
    }

    [Fact]
    public async Task GetCompanyAdoptionReport_AppliesFiltersCorrectly()
    {
        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseInMemoryDatabase($"adoption-report-filter-{Guid.NewGuid():N}").Options;
        using var context = new CatalogDbContext(options);

        context.Domains.Add(new TmDominio { Id = 1, Dominio = "Application Security" });
        context.BuildingBlocks.Add(new TBuildingBlock { Id = 10, IdDominio = 1, Nombre = "SAST" });
        context.Companies.AddRange(
            new TEmpresaSubsidiaria { Id = 100, Nombre = "BCP", Pais = "Perú" },
            new TEmpresaSubsidiaria { Id = 200, Nombre = "Mibanco", Pais = "Perú" }
        );
        context.Technologies.Add(new TTecnologiaTSI { Id = 1000, NombreCorporativo = "Checkmarx" });
        context.StandardTechnologyHistories.Add(new TEstandarTecnologiaHistorico
        {
            IdEstandarTecnologia = 1,
            IdBuildingBlock = 10,
            IdTecnologiaTSI = 1000,
            RolEstandar = "PRINCIPAL",
            EstadoVigencia = "ACTIVO_VIGENTE"
        });
        context.AdoptionProcesses.Add(new TProcesoAdopcionTSI { IdProcesoAdopcionTSI = 1, IdBuildingBlock = 10, CodigoProceso = "P1", NombreProceso = "N1" });
        context.AdoptionProcessCompanies.AddRange(
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 1, IdProcesoAdopcionTSI = 1, IdEmpresaSubsidiaria = 100, Aplica = true },
            new TProcesoAdopcionEmpresa { IdProcesoAdopcionEmpresa = 2, IdProcesoAdopcionTSI = 1, IdEmpresaSubsidiaria = 200, Aplica = false, JustificacionNoAplica = "Justif" }
        );
        context.ImplementedTechnologies.Add(new TTecnologiaTSIimplementadaSubsidiaria
        {
            IdTecnologiaTSIimplementadaSubsidiaria = 1,
            IdEmpresaSubsidiaria = 100,
            IdBuildingBlock = 10,
            IdTecnologiaTSI = 1000,
            EsTecnologiaPrimaria = true
        });
        await context.SaveChangesAsync();

        var service = new CatalogReportingService(new FakeCatalogService(), null, context);

        // Search Filter
        var searchReport = await service.GetCompanyAdoptionReportAsync(new CompanyAdoptionReportQuery(Search: "BCP"));
        Assert.Single(searchReport.AlignmentRows);
        Assert.Equal("BCP", searchReport.AlignmentRows[0].EmpresaNombre);

        // AlignmentState Filter
        var noAplicaReport = await service.GetCompanyAdoptionReportAsync(new CompanyAdoptionReportQuery(AlignmentState: "NO_APLICA"));
        Assert.Single(noAplicaReport.AlignmentRows);
        Assert.Equal("Mibanco", noAplicaReport.AlignmentRows[0].EmpresaNombre);
    }

    [Fact]
    public async Task GetCompanyAdoptionReport_ThrowsWhenContextNull()
    {
        var service = new CatalogReportingService(new FakeCatalogService(), null, null);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GetCompanyAdoptionReportAsync(new CompanyAdoptionReportQuery()));
    }

    private sealed class FakeCatalogService : ICatalogManagementService
    {
        public Task<CatalogPageResult> ListAsync(MasterCatalogDefinition definition, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null) => Task.FromResult(new CatalogPageResult([], 1, pageSize, 0));
        public Task<CatalogPageResult> ListRelatedAsync(MasterCatalogDefinition definition, CatalogColumnDefinition foreignKey, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default) => Task.FromResult(new CatalogPageResult([], 1, pageSize, 0));
        public Task<IReadOnlyList<CatalogRelationBucket>> GetRelationCountsAsync(MasterCatalogDefinition parent, MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<CatalogRelationBucket>>([]);
        public Task<int> GetRelatedCountAsync(MasterCatalogDefinition child, CatalogColumnDefinition foreignKey, int parentId, CancellationToken cancellationToken = default) => Task.FromResult(0);
        public Task<CatalogRow?> GetAsync(MasterCatalogDefinition definition, int id, CancellationToken cancellationToken = default) => Task.FromResult<CatalogRow?>(null);
        public Task<IReadOnlyDictionary<string, IReadOnlyList<CatalogOption>>> GetOptionsAsync(MasterCatalogDefinition definition, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<int> CreateAsync(MasterCatalogDefinition definition, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> UpdateAsync(MasterCatalogDefinition definition, int id, IReadOnlyDictionary<string, string?> values, Guid actorUserId, string correlationId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<int, int>> GetVendorTechnologyCountsAsync(IEnumerable<int> vendorIds, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyDictionary<int, int>>(new Dictionary<int, int>());
        public Task<IReadOnlyList<VendorTechnologyDto>> GetVendorTechnologiesAsync(int vendorId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<VendorTechnologyDto>>([]);
    }
}