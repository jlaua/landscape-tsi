namespace Landscape.Tsi.Application.Catalogs;

public sealed record CatalogPageResult(IReadOnlyList<CatalogRow> Items, int Page, int PageSize, int TotalCount)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
