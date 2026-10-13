using System.Text.Json;
using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Application.Mcp.Tools;

public sealed class GetCatalogDetailTool : IMcpTool
{
    private readonly ICatalogManagementService _catalogService;
    private readonly IBuildingBlockRelatedService _buildingBlockRelatedService;

    public GetCatalogDetailTool(
        ICatalogManagementService catalogService,
        IBuildingBlockRelatedService buildingBlockRelatedService)
    {
        _catalogService = catalogService;
        _buildingBlockRelatedService = buildingBlockRelatedService;
    }

    public string Name => "get_catalog_detail";

    public string Description =>
        "Obtiene el detalle completo de un registro específico en un catálogo por su identificador numérico (ID). Si el catálogo es 'building-block', incluye automáticamente la lista completa de sus capacidades de seguridad asociadas con sus estados (Cubierto, Parcialmente, No Cubierto), cantidad de funcionalidades y tecnologías relacionadas. Excluye automáticamente información sensible.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            catalog = new
            {
                type = "string",
                description = "Código del catálogo (ej: 'dominio', 'building-block', 'capacidad-seguridad', 'tecnologia-tsi', 'funcionalidad', 'empresa-subsidiaria')."
            },
            id = new
            {
                type = "integer",
                description = "Identificador numérico del elemento."
            }
        },
        required = new[] { "catalog", "id" }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments, McpContext context, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("catalog", out var catalogProp) || catalogProp.GetString() is not { } catalogCode)
        {
            return McpToolResult.FromText("Parámetro 'catalog' es requerido.", isError: true);
        }

        if (!arguments.TryGetProperty("id", out var idProp) || !idProp.TryGetInt32(out var id))
        {
            return McpToolResult.FromText("Parámetro 'id' es requerido y debe ser entero.", isError: true);
        }

        var catalogDef = MasterCatalogRegistry.Catalogs.FirstOrDefault(c =>
            c.Code.Equals(catalogCode, StringComparison.OrdinalIgnoreCase) ||
            c.Route.Equals(catalogCode, StringComparison.OrdinalIgnoreCase));

        if (catalogDef is null)
        {
            return McpToolResult.FromText($"Catálogo '{catalogCode}' no encontrado.", isError: true);
        }

        var row = await _catalogService.GetAsync(catalogDef, id, cancellationToken);
        if (row is null)
        {
            return McpToolResult.FromText($"No se encontró ningún elemento con ID {id} en el catálogo '{catalogCode}'.", isError: true);
        }

        var sanitizedValues = new Dictionary<string, object?>();
        foreach (var col in catalogDef.Columns)
        {
            if (CatalogDisplayPrivacy.IsSensitive(catalogDef, col.Code))
            {
                continue;
            }

            if (row.Values.TryGetValue(col.Code, out var val))
            {
                sanitizedValues[col.Code] = val;
            }
        }

        var displayName = row.DisplayValues.TryGetValue(catalogDef.DisplayColumnCode, out var dispVal) && !string.IsNullOrWhiteSpace(dispVal)
            ? dispVal
            : row.Id.ToString();

        // Si es un Building Block, incluir automáticamente sus capacidades de seguridad y entidades relacionadas
        if (catalogDef.Code.Equals("building-block", StringComparison.OrdinalIgnoreCase))
        {
            var related = await _buildingBlockRelatedService.GetAsync(
                buildingBlockId: id,
                capabilitySearch: null,
                functionalitySearch: null,
                technologySearch: null,
                capabilityPage: 1,
                functionalityPage: 1,
                technologyPage: 1,
                pageSize: 100,
                cancellationToken: cancellationToken);

            var capacidades = related.Capabilities.Items.Select(c => new
            {
                id = c.Id,
                nombre = c.Values.TryGetValue("capacidad", out var cv) && cv is not null ? cv.ToString() : c.Id.ToString(),
                estado = c.Values.TryGetValue("estado", out var ev) ? ev?.ToString() : null,
                descripcion = c.Values.TryGetValue("descripcion", out var dv) ? dv?.ToString() : null
            }).ToList();

            var tecnologias = related.Technologies.Items.Select(t => new
            {
                id = t.Id,
                tecnologia = t.Values.TryGetValue("tecnologia", out var tv) && tv is not null ? tv.ToString() : t.Id.ToString()
            }).ToList();

            var bbResult = new
            {
                catalog = catalogDef.Code,
                catalogName = catalogDef.Name,
                id = row.Id,
                displayName,
                details = sanitizedValues,
                totalCapacidades = related.Capabilities.TotalCount,
                capacidadesSeguridad = capacidades,
                totalFuncionalidades = related.Functionalities.TotalCount,
                totalTecnologiasRelacionadas = related.Technologies.TotalCount,
                tecnologiasRelacionadas = tecnologias
            };

            return McpToolResult.FromJson(bbResult);
        }

        var result = new
        {
            catalog = catalogDef.Code,
            catalogName = catalogDef.Name,
            id = row.Id,
            displayName,
            details = sanitizedValues
        };

        return McpToolResult.FromJson(result);
    }
}
