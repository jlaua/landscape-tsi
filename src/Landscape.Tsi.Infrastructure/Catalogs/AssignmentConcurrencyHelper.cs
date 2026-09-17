using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Landscape.Tsi.Infrastructure.Catalogs;

public static class AssignmentConcurrencyHelper
{
    public static string CreateCapabilityToken(int id, int? buildingBlockId, string? name, int? stateId)
    {
        var dict = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["idBuildingBlock"] = buildingBlockId,
            ["nombre"] = name?.Trim() ?? string.Empty,
            ["idEstado"] = stateId
        };
        return ComputeHash(dict);
    }

    public static string CreateFunctionalityToken(int id, int? capabilityId, string? name, int? stateId)
    {
        var dict = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["idCapacidad"] = capabilityId,
            ["nombre"] = name?.Trim() ?? string.Empty,
            ["idEstado"] = stateId
        };
        return ComputeHash(dict);
    }

    public static bool VerifyCapabilityToken(string providedToken, int id, int? buildingBlockId, string? name, int? stateId)
    {
        if (string.IsNullOrWhiteSpace(providedToken)) return false;
        var expected = CreateCapabilityToken(id, buildingBlockId, name, stateId);
        return string.Equals(providedToken.Trim(), expected, StringComparison.Ordinal);
    }

    public static bool VerifyFunctionalityToken(string providedToken, int id, int? capabilityId, string? name, int? stateId)
    {
        if (string.IsNullOrWhiteSpace(providedToken)) return false;
        var expected = CreateFunctionalityToken(id, capabilityId, name, stateId);
        return string.Equals(providedToken.Trim(), expected, StringComparison.Ordinal);
    }

    private static string ComputeHash(Dictionary<string, object?> values)
    {
        var payload = JsonSerializer.Serialize(values.OrderBy(item => item.Key).ToDictionary(item => item.Key, item => item.Value));
        return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }
}