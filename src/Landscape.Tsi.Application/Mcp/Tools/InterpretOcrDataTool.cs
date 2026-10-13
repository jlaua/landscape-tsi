using System.Text.Json;
using Landscape.Tsi.Application.Catalogs;

namespace Landscape.Tsi.Application.Mcp.Tools;

public sealed class InterpretOcrDataTool : IMcpTool
{
    public string Name => "interpret_ocr_data";

    public string Description =>
        "Recibe texto en bruto extraído de una imagen, tabla o escaneo OCR por el asistente y lo interpreta mapeando los campos a las tablas destino (Dominio, Building Block, Funcionalidad, Contactos o Drivers). Devuelve los campos estructurados para solicitar confirmación al usuario antes de registrar.";

    public object InputSchema => new
    {
        type = "object",
        properties = new
        {
            rawText = new
            {
                type = "string",
                description = "Texto plano o contenido tabulado extraído de la imagen u OCR."
            },
            targetEntitySuggestion = new
            {
                type = "string",
                description = "Sugerencia opcional de la entidad destino si ya se conoce (ej: 'dominio', 'building-block', 'funcionalidad', 'driver', 'contacto-vendor')."
            }
        },
        required = new[] { "rawText" }
    };

    public Task<McpToolResult> ExecuteAsync(JsonElement arguments, McpContext context, CancellationToken cancellationToken = default)
    {
        if (!arguments.TryGetProperty("rawText", out var rawTextProp) || rawTextProp.GetString() is not { } rawText || string.IsNullOrWhiteSpace(rawText))
        {
            return Task.FromResult(McpToolResult.FromText("El parámetro 'rawText' es requerido y no puede estar vacío.", isError: true));
        }

        var suggestion = arguments.TryGetProperty("targetEntitySuggestion", out var s) ? s.GetString() : null;
        var detectedEntity = suggestion;

        if (string.IsNullOrWhiteSpace(detectedEntity))
        {
            var lower = rawText.ToLowerInvariant();
            if (lower.Contains("driver") || lower.Contains("volumetria") || lower.Contains("unidad de medida"))
            {
                detectedEntity = "driver";
            }
            else if (lower.Contains("building block") || lower.Contains("pilar zt") || lower.Contains("fase de adopcion"))
            {
                detectedEntity = "building-block";
            }
            else if (lower.Contains("dominio") || lower.Contains("subdominio cvt") || lower.Contains("dimension"))
            {
                detectedEntity = "dominio";
            }
            else if (lower.Contains("funcionalidad") || lower.Contains("cobertura"))
            {
                detectedEntity = "funcionalidad";
            }
            else if (lower.Contains("vendor") || lower.Contains("proveedor"))
            {
                detectedEntity = "contacto-vendor";
            }
            else if (lower.Contains("partner") || lower.Contains("canal"))
            {
                detectedEntity = "contacto-partner";
            }
            else if (lower.Contains("especialista") || lower.Contains("subsidiaria"))
            {
                detectedEntity = "contacto-empresa";
            }
            else
            {
                detectedEntity = "dominio";
            }
        }

        var lines = rawText.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var mappedFields = new Dictionary<string, string>();

        foreach (var line in lines)
        {
            var colonIndex = line.IndexOfAny(new[] { ':', '=', '-' });
            if (colonIndex > 0)
            {
                var key = line[..colonIndex].Trim();
                var val = line[(colonIndex + 1)..].Trim();
                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(val))
                {
                    mappedFields[key] = val;
                }
            }
        }

        var catalogDef = MasterCatalogRegistry.Catalogs.FirstOrDefault(c => c.Code.Equals(detectedEntity, StringComparison.OrdinalIgnoreCase));

        var response = new
        {
            entidadDestinoDetectada = detectedEntity,
            nombreEntidad = catalogDef?.Name ?? detectedEntity,
            camposInterpretados = mappedFields,
            totalLineasProcesadas = lines.Length,
            instruccion = $"Muestra estos campos estructurados al usuario y pregúntale: '¿Está bien la interpretación para {catalogDef?.Name ?? detectedEntity}? ¿Deseas proceder con el registro?'",
            siguientePaso = "Una vez el usuario apruebe, utiliza 'mutate_entity' con stage='preview' o stage='execute'."
        };

        return Task.FromResult(McpToolResult.FromJson(response));
    }
}
