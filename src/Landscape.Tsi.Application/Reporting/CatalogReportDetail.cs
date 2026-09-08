namespace Landscape.Tsi.Application.Reporting;

public sealed record CatalogReportColumn(string Code, string Label);
public sealed record CatalogReportDetailRow(int Id, IReadOnlyDictionary<string, string?> Values);
public sealed record CatalogReportDetail(string Code, string Name, int Page, int PageSize, int TotalCount, IReadOnlyList<CatalogReportColumn> Columns, IReadOnlyList<CatalogReportDetailRow> Rows)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}
public sealed record CatalogReportRelation(string ChildCode, string ChildName, string Type, IReadOnlyList<CatalogReportRelationBucket> Buckets);
public sealed record CatalogReportRelationBucket(int ParentId, string ParentName, int Total);
public sealed record CatalogContextKpi(string Code, string Label, int Value, string RelationCode);
