namespace Landscape.Tsi.Web.Models;

public sealed class CatalogInputModel
{
    public Dictionary<string, string?> Values { get; set; } = new(StringComparer.Ordinal);
}
