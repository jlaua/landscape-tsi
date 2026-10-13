using System.Text.Json;
using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Application.Mcp.Tools;

public sealed class SearchCatalogsTool : IMcpTool
{
    private readonly ICatalogManagementService _catalogService;

    public SearchCatalogsTool(ICatalogManagementService catalogService)
    {
        _catalogService = catalogService;
    }

    public string Name => "search_catalogs";

    public string Description =>
        "Busca elementos en los catálogos de Landscape TSI (dominio, building-block, capacidad-seguridad, funcionalidad, familia, tecnologia-tsi, empresa-subsidiaria, driver). Excluye automáticamente información sensible como teléfonos o correos.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            catalog = new
            {
                type = "string",
                description = "Código del catálogo a consultar. Opciones: 'dominio', 'building-block', 'capacidad-seguridad', 'funcionalidad', 'familia', 'tecnologia-tsi', 'empresa-subsidiaria', 'driver'.",
                @enum = new[] { "dominio", "building-block", "capacidad-seguridad", "funcionalidad", "familia", "tecnologia-tsi", "empresa-subsidiaria", "driver" }
            },
            search = new
            {
                type = "string",
                description = "Término de búsqueda o palabra clave para filtrar."
            },
            page = new
            {
                type = "integer",
                description = "Número de página (por defecto 1)."
            },
            pageSize = new
            {
                type = "integer",
                description = "Cantidad de resultados por página (por defecto 20, máximo 50)."
            }
        },
        required = new[] { "catalog" }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments, McpContext context, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("catalog", out var catalogProp) || catalogProp.GetString() is not { } catalogCode)
        {
            return McpToolResult.FromText("Parámetro 'catalog' es requerido.", isError: true);
        }

        var catalogDef = MasterCatalogRegistry.Catalogs.FirstOrDefault(c =>
            c.Code.Equals(catalogCode, StringComparison.OrdinalIgnoreCase) ||
            c.Route.Equals(catalogCode, StringComparison.OrdinalIgnoreCase));

        if (catalogDef is null)
        {
            return McpToolResult.FromText($"Catálogo '{catalogCode}' no encontrado en el registro maestro.", isError: true);
        }

        var search = arguments.TryGetProperty("search", out var s) ? s.GetString() : null;
        var page = arguments.TryGetProperty("page", out var p) && p.TryGetInt32(out var pVal) && pVal > 0 ? pVal : 1;
        var pageSize = arguments.TryGetProperty("pageSize", out var ps) && ps.TryGetInt32(out var psVal) && psVal > 0 ? Math.Min(psVal, 50) : 20;

        var result = await _catalogService.ListAsync(catalogDef, search, page, pageSize, cancellationToken);

        var sanitizedItems = result.Items.Select(row =>
        {
            var sanitizedValues = new Dictionary<string, object?>();
            foreach (var col in catalogDef.Columns)
            {
                if (CatalogDisplayPrivacy.IsSensitive(catalogDef, col.Code))
                {
                    continue; // Omitir datos sensibles completamente para la IA
                }

                if (row.Values.TryGetValue(col.Code, out var val))
                {
                    sanitizedValues[col.Code] = val;
                }
            }

            var display = row.DisplayValues.TryGetValue(catalogDef.DisplayColumnCode, out var dispVal) && !string.IsNullOrWhiteSpace(dispVal)
                ? dispVal
                : row.Id.ToString();

            return new
            {
                id = row.Id,
                display,
                values = sanitizedValues
            };
        }).ToList();

        var response = new
        {
            catalog = catalogDef.Code,
            catalogName = catalogDef.Name,
            page = result.Page,
            pageSize = result.PageSize,
            totalItems = result.TotalCount,
            totalPages = result.TotalPages,
            items = sanitizedItems
        };

        return McpToolResult.FromJson(response);
    }
}
