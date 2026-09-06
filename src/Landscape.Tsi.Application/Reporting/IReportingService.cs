namespace Landscape.Tsi.Application.Reporting;

public interface IReportingService
{
    Task<IReadOnlyList<CatalogReportPoint>> GetCatalogTotalsAsync(string? group, CancellationToken cancellationToken = default);
}
