namespace Landscape.Tsi.Application.Catalogs;

public sealed record CatalogRow(
    int Id,
    IReadOnlyDictionary<string, object?> Values,
    IReadOnlyDictionary<string, string?> DisplayValues);