using System.Text.Json;
using Landscape.Tsi.Application.Adoption;
using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Application.Mcp.Tools;

public sealed class EntityMutationTool : IMcpTool
{
    private static readonly Guid AiAgentActorUserId = Guid.Parse("00000000-0000-0000-0000-0000000000A1");
    private readonly ICatalogManagementService _catalogService;
    private readonly IAdoptionProcessService _adoptionService;

    private static readonly HashSet<string> AllowedEntities = new(StringComparer.OrdinalIgnoreCase)
    {
        "dominio",
        "building-block",
        "funcionalidad",
        "contacto-vendor",
        "contacto-partner",
        "contacto-empresa",
        "driver"
    };

    public EntityMutationTool(
        ICatalogManagementService catalogService,
        IAdoptionProcessService adoptionService)
    {
        _catalogService = catalogService;
        _adoptionService = adoptionService;
    }

    public string Name => "mutate_entity";

    public string Description =>
        "Permite registrar o actualizar entidades permitidas: dominio, building-block, funcionalidad, contacto-vendor, contacto-partner, contacto-empresa y driver. Implementa flujo de dos pasos: primero usa stage='preview' para mostrar los datos interpretados al usuario y solicitar confirmación humana; solo cuando el usuario confirme en el chat, invoca stage='execute' con confirm=true.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            entity = new
            {
                type = "string",
                description = "Tipo de entidad a registrar/actualizar.",
                @enum = new[] { "dominio", "building-block", "funcionalidad", "contacto-vendor", "contacto-partner", "contacto-empresa", "driver" }
            },
            action = new
            {
                type = "string",
                description = "Acción a realizar: 'create' o 'update'.",
                @enum = new[] { "create", "update" }
            },
            id = new
            {
                type = "integer",
                description = "ID del registro si la acción es 'update'."
            },
            fields = new
            {
                type = "object",
                description = "Diccionario con los campos y valores de la entidad (ej: nombre, descripcion, idDominio, etc.)."
            },
            stage = new
            {
                type = "string",
                description = "Etapa de la operación: 'preview' (para mostrar al usuario y pedir confirmación) o 'execute' (para guardar definitivamente). Por defecto 'preview'.",
                @enum = new[] { "preview", "execute" }
            },
            confirm = new
            {
                type = "boolean",
                description = "Debe ser true en stage='execute' para proceder tras la aprobación del usuario."
            }
        },
        required = new[] { "entity", "action", "fields" }
    };

    public async Task<McpToolResult> ExecuteAsync(JsonElement arguments, McpContext context, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("entity", out var entityProp) || entityProp.GetString() is not { } entityCode)
        {
            return McpToolResult.FromText("El parámetro 'entity' es requerido.", isError: true);
        }

        if (!AllowedEntities.Contains(entityCode))
        {
            return McpToolResult.FromText($"La entidad '{entityCode}' no está autorizada para mutaciones por asistente de IA. Entidades permitidas: {string.Join(", ", AllowedEntities)}", isError: true);
        }

        var action = arguments.TryGetProperty("action", out var a) ? a.GetString()?.ToLowerInvariant() : "create";
        var stage = arguments.TryGetProperty("stage", out var st) ? st.GetString()?.ToLowerInvariant() : "preview";
        var isConfirmed = arguments.TryGetProperty("confirm", out var c) && c.GetBoolean();
        var id = arguments.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out var idVal) ? idVal : (int?)null;

        if (action == "update" && (!id.HasValue || id.Value <= 0))
        {
            return McpToolResult.FromText("Para la acción 'update' se requiere especificar un 'id' válido mayor a cero.", isError: true);
        }

        // Convert fields JsonElement to string Dictionary
        var fieldValues = new Dictionary<string, string?>();
        if (arguments.TryGetProperty("fields", out var fieldsElem) && fieldsElem.ValueKind == JsonValueKind.Object)
        {
            foreach (var prop in fieldsElem.EnumerateObject())
            {
                fieldValues[prop.Name] = prop.Value.ValueKind switch
                {
                    JsonValueKind.Null => null,
                    JsonValueKind.String => prop.Value.GetString(),
                    _ => prop.Value.ToString()
                };
            }
        }
        else
        {
            return McpToolResult.FromText("El parámetro 'fields' debe ser un objeto con los valores a registrar.", isError: true);
        }

        var catalogDef = MasterCatalogRegistry.Catalogs.FirstOrDefault(cat =>
            cat.Code.Equals(entityCode, StringComparison.OrdinalIgnoreCase) ||
            cat.Route.Equals(entityCode, StringComparison.OrdinalIgnoreCase));

        // Manejo de la etapa PREVIEW
        if (stage != "execute" || !isConfirmed)
        {
            return McpToolResult.FromJson(new
            {
                status = "REQUIRES_USER_CONFIRMATION",
                mensaje = "Se ha generado la vista previa del registro. Muestra este resumen al usuario y solicita su confirmación explícita antes de ejecutar el guardado definitivo.",
                entidad = entityCode,
                accion = action,
                idObjetivo = id,
                datosAProcesar = fieldValues,
                instruccionParaAsistente = "Pregunta al usuario: '¿Confirmas el registro/actualización con estos datos?' y una vez confirmado ejecuta esta herramienta con stage='execute' y confirm=true."
            });
        }

        // Manejo de la etapa EXECUTE (Usuario confirmó)
        var actorId = context.ActorUserId ?? AiAgentActorUserId;
        var correlationId = string.IsNullOrWhiteSpace(context.CorrelationId) ? Guid.NewGuid().ToString("N") : context.CorrelationId;

        if (catalogDef is not null)
        {
            if (action == "create")
            {
                var newId = await _catalogService.CreateAsync(catalogDef, fieldValues, actorId, correlationId, cancellationToken);
                return McpToolResult.FromJson(new
                {
                    status = "SUCCESS",
                    mensaje = $"Registro creado exitosamente en '{catalogDef.Name}'.",
                    idGenerado = newId,
                    entidad = catalogDef.Code
                });
            }
            else
            {
                var updated = await _catalogService.UpdateAsync(catalogDef, id!.Value, fieldValues, actorId, correlationId, cancellationToken);
                return McpToolResult.FromJson(new
                {
                    status = updated ? "SUCCESS" : "NOT_FOUND",
                    mensaje = updated ? $"Registro #{id} actualizado exitosamente en '{catalogDef.Name}'." : $"No se encontró el registro #{id}.",
                    id = id!.Value,
                    entidad = catalogDef.Code
                });
            }
        }

        return McpToolResult.FromText($"No se pudo resolver el catálogo maestro para '{entityCode}'.", isError: true);
    }
}
