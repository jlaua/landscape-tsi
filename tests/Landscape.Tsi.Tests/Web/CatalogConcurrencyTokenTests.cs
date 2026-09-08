using Landscape.Tsi.Application.Catalogs;
using Landscape.Tsi.Web.Models;

namespace Landscape.Tsi.Tests.Web;

public sealed class CatalogConcurrencyTokenTests
{
    [Fact]
    public void Token_IsStableForEquivalentValuesAndChangesWhenAValueChanges()
    {
        var first = new Dictionary<string, object?> { ["nombre"] = "A", ["orden"] = 1 };
        var reordered = new Dictionary<string, object?> { ["orden"] = 1, ["nombre"] = "A" };
        var changed = new Dictionary<string, object?> { ["nombre"] = "B", ["orden"] = 1 };

        Assert.Equal(CatalogConcurrencyToken.Create(first), CatalogConcurrencyToken.Create(reordered));
        Assert.NotEqual(CatalogConcurrencyToken.Create(first), CatalogConcurrencyToken.Create(changed));
    }
}
