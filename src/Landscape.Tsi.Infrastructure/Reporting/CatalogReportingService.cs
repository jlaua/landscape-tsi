using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Application.Reporting;

namespace Landscape.Tsi.Infrastructure.Reporting;

public sealed class CatalogReportingService(ICatalogManagementService catalogs) : IReportingService
{
    public async Task<IReadOnlyList<CatalogReportPoint>> GetCatalogTotalsAsync(string? group, CancellationToken cancellationToken = default)
    {
        var definitions = MasterCatalogRegistry.Catalogs
            .Where(definition => definition.Enabled && (string.IsNullOrWhiteSpace(group) || string.Equals(definition.Group, group, StringComparison.OrdinalIgnoreCase)))
            .ToArray();
        var result = new List<CatalogReportPoint>(definitions.Length);
        foreach (var definition in definitions)
        {
            var page = await catalogs.ListAsync(definition, null, 1, 1, cancellationToken);
            result.Add(new CatalogReportPoint(definition.Code, definition.Name, definition.Group, page.TotalCount));
        }

        return result;
    }
}
