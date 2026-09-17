namespace Landscape.Tsi.Application.Reporting;

public interface IReportingService
{
    Task<IReadOnlyList<CatalogReportPoint>> GetCatalogTotalsAsync(string? group, CancellationToken cancellationToken = default);
    Task<CatalogReportDetail> GetCatalogDetailAsync(string catalogCode, string? search, int page, int pageSize, CancellationToken cancellationToken = default, string? sortColumn = null, string? sortDirection = null);
    Task<IReadOnlyList<CatalogReportRelation>> GetCatalogRelationsAsync(string catalogCode, CancellationToken cancellationToken = default);
    Task<CatalogReportDetail> GetRelatedCatalogDetailAsync(string parentCode, string childCode, int parentId, string? search, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogContextKpi>> GetCatalogContextKpisAsync(string catalogCode, int recordId, CancellationToken cancellationToken = default);
    Task<CompanyCisoReport> GetCompanyCisoReportAsync(CompanyCisoReportQuery query, CancellationToken cancellationToken = default);
    Task<CompanyAdoptionReport> GetCompanyAdoptionReportAsync(CompanyAdoptionReportQuery query, CancellationToken cancellationToken = default);
}