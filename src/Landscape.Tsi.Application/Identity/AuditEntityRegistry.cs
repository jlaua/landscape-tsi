using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Application.Identity;

public static class AuditEntityRegistry
{
    public static IReadOnlyList<AuditFilterOption> VisibleEntities { get; } =
        MasterCatalogRegistry.EntityMetadata
            .Where(entity => entity.IsAdministrable)
            .Select(entity => new AuditFilterOption(entity.Code, entity.LogicalName))
            .Append(new AuditFilterOption("bridge-building-technology", "Mapeo Building Block / Tecnología TSI"))
            .OrderBy(entity => entity.DisplayName, StringComparer.Ordinal)
            .ToArray();

    public static bool TryGetPhysicalTable(string? code, out string? physicalTableName)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            physicalTableName = null;
            return true;
        }

        if (code == "bridge-building-technology")
        {
            physicalTableName = "TBuildingBlockVsTTecnologiaTSI";
            return true;
        }

        physicalTableName = MasterCatalogRegistry.EntityMetadata
            .FirstOrDefault(entity => entity.Code == code && entity.IsAdministrable)
            ?.PhysicalTableName;
        return physicalTableName is not null;
    }

    public static string DisplayName(string? physicalTableName)
        => MasterCatalogRegistry.EntityMetadata
               .FirstOrDefault(entity => entity.PhysicalTableName == physicalTableName)
               ?.LogicalName
           ?? (physicalTableName == "TBuildingBlockVsTTecnologiaTSI"
               ? "Mapeo Building Block / Tecnología TSI"
               : physicalTableName ?? "—");
}
