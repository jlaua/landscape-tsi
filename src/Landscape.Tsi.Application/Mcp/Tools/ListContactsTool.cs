using System.Text.Json;
using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Application.Mcp.Tools;

public sealed class ListContactsTool : IMcpTool
{
    private readonly ICatalogManagementService _catalogService;

    public ListContactsTool(ICatalogManagementService catalogService)
    {
        _catalogService = catalogService;
    }

    public string Name => "list_contacts";

    public string Description =>
        "Consulta contactos de vendors, partners, especialistas de empresas subsidiarias y CISOs. Por política estricta de seguridad de Landscape TSI, NUNCA se devuelven correos electrónicos ni teléfonos, únicamente el nombre, rol/cargo y la entidad/empresa a la que pertenece.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            type = new
            {
                type = "string",
                description = "Tipo de contacto a consultar. Opciones: 'vendor', 'partner', 'especialista_empresa', 'ciso', 'todos'. Por defecto 'todos'.",
                @enum = new[] { "vendor", "partner", "especialista_empresa", "ciso", "todos" }
            },
            search = new
            {
                type = "string",
                description = "Filtro de búsqueda por nombre o empresa."
            },
            page = new
            {
                type = "integer",
                description = "Número de página (por defecto 1)."
            },
            pageSize = new
            {
                type = "integer",
                description = "Cantidad de resultados por página (por defecto 20)."
            }
        }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments, McpContext context, CancellationToken cancellationToken = default)
    {
        var type = arguments.TryGetProperty("type", out var t) ? t.GetString() ?? "todos" : "todos";
        var search = arguments.TryGetProperty("search", out var s) ? s.GetString() : null;
        var page = arguments.TryGetProperty("page", out var p) && p.TryGetInt32(out var pVal) && pVal > 0 ? pVal : 1;
        var pageSize = arguments.TryGetProperty("pageSize", out var ps) && ps.TryGetInt32(out var psVal) && psVal > 0 ? Math.Min(psVal, 50) : 20;

        var catalogCodesToQuery = type switch
        {
            "vendor" => new[] { "contacto-vendor" },
            "partner" => new[] { "contacto-partner" },
            "especialista_empresa" => new[] { "contacto-empresa" },
            "ciso" => new[] { "ciso" },
            _ => new[] { "contacto-vendor", "contacto-partner", "contacto-empresa", "ciso" }
        };

        var allContacts = new List<object>();

        foreach (var code in catalogCodesToQuery)
        {
            var def = MasterCatalogRegistry.Catalogs.FirstOrDefault(c => c.Code == code);
            if (def is null)
            {
                continue;
            }

            var paged = await _catalogService.ListAsync(def, search, 1, 100, cancellationToken);
            foreach (var row in paged.Items)
            {
                var role = row.Values.TryGetValue("rol", out var rVal) ? rVal?.ToString() : null;
                var org = code switch
                {
                    "contacto-vendor" => row.Values.TryGetValue("vendor", out var v) ? v?.ToString() : "Vendor",
                    "contacto-partner" => row.Values.TryGetValue("partner", out var pObj) ? pObj?.ToString() : "Partner",
                    "contacto-empresa" => row.Values.TryGetValue("empresa", out var e) ? e?.ToString() : "Empresa Subsidiaria",
                    "ciso" => row.Values.TryGetValue("empresa", out var cEmp) ? cEmp?.ToString() : "Empresa Subsidiaria",
                    _ => null
                };

                string contactRole = code switch
                {
                    "ciso" => "CISO",
                    _ => role ?? "Especialista / Contacto"
                };

                var contactName = row.DisplayValues.TryGetValue(def.DisplayColumnCode, out var d) && !string.IsNullOrWhiteSpace(d)
                    ? d
                    : row.Id.ToString();

                allContacts.Add(new
                {
                    id = row.Id,
                    tipo = code,
                    nombre = contactName,
                    organizacion = org,
                    rol = contactRole,
                    especialidad = row.Values.TryGetValue("especialistaEnQuePlataformas", out var esp) ? esp?.ToString() : null
                });
            }
        }

        var pagedContacts = allContacts.Skip((page - 1) * pageSize).Take(pageSize).ToList();

        var result = new
        {
            page,
            pageSize,
            totalItems = allContacts.Count,
            totalPages = (int)Math.Ceiling(allContacts.Count / (double)pageSize),
            contacts = pagedContacts
        };

        return McpToolResult.FromJson(result);
    }
}
