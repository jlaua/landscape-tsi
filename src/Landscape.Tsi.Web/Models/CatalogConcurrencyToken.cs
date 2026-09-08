using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Web.Models;

public static class CatalogConcurrencyToken
{
    public static string Create(CatalogRow record) => Create(record.Values);

    public static string Create(IReadOnlyDictionary<string, object?> values)
    {
        var payload = JsonSerializer.Serialize(values.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value));
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}