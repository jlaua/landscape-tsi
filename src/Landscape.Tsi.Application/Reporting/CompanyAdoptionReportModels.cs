using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Application.Reporting;

public sealed record CompanyAdoptionReportQuery(
    string? Search = null,
    int? DominioId = null,
    int? BuildingBlockId = null,
    int? EmpresaId = null,
    string? AlignmentState = null,
    int Page = 1,
    int PageSize = 25);

public sealed record CompanyAdoptionSummaryKpis(
    int TotalEmpresasParticipantes,
    int TotalAlineadas,
    int TotalHomologadas,
    int TotalNoAlineadas,
    int TotalNoAplica,
    int TotalPendientes)
{
    public decimal PorcentajeAlineacionGlobal =>
        (TotalAlineadas + TotalHomologadas + TotalNoAlineadas + TotalPendientes) > 0
            ? Math.Round(((decimal)(TotalAlineadas + TotalHomologadas) / (TotalAlineadas + TotalHomologadas + TotalNoAlineadas + TotalPendientes)) * 100m, 1)
            : 0m;
}

public sealed record DomainAdoptionMetric(
    int DominioId,
    string DominioNombre,
    int TotalBuildingBlocks,
    int TotalEmpresasConAdopcion,
    IReadOnlyList<BuildingBlockAdoptionMetric> BuildingBlocksCoverage);

public sealed record BuildingBlockAdoptionMetric(
    int BuildingBlockId,
    string BuildingBlockNombre,
    int DominioId,
    string DominioNombre,
    int EmpresasConvocadas,
    int EmpresasConAdopcion,
    IReadOnlyList<string> EmpresasNombres,
    string? TecnologiaEstandarCorporativa);

public sealed record CompanyTechnologyAlignmentRow(
    int EmpresaId,
    string EmpresaNombre,
    string? Pais,
    string DominioNombre,
    int BuildingBlockId,
    string BuildingBlockNombre,
    string? TecnologiaEstandarCorporativa,
    string? TecnologiaLocalImplementada,
    string? VersionDesplegada,
    string? VendorLocal,
    string? NumeroContratoLocal,
    DateTime? FechaFinContratoLocal,
    string EstadoAlineacion,
    string? JustificacionNoAplica);

public sealed record CompanyAdoptionReport(
    CompanyAdoptionReportQuery Query,
    CompanyAdoptionSummaryKpis Kpis,
    IReadOnlyList<DomainAdoptionMetric> DomainMetrics,
    IReadOnlyList<CompanyTechnologyAlignmentRow> AlignmentRows,
    int TotalAlignmentRows,
    int TotalPages,
    IReadOnlyList<CatalogOption> DominiosFilter,
    IReadOnlyList<CatalogOption> BuildingBlocksFilter,
    IReadOnlyList<CatalogOption> EmpresasFilter);