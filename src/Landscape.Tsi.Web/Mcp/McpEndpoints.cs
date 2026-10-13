using System.Text.Json;
using Landscape.Tsi.Application.Mcp;
using Microsoft.AspNetCore.Mvc;

namespace Landscape.Tsi.Web.Mcp;

public static class McpEndpoints
{
    public static IEndpointRouteBuilder MapMcpEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var mcpGroup = endpoints.MapGroup("/api/mcp");

        // Health endpoint
        mcpGroup.MapGet("/health", () => Results.Ok(new
        {
            status = "Healthy",
            protocol = "Model Context Protocol (MCP)",
            version = "2024-11-05",
            timestamp = DateTime.UtcNow
        }));

        // Tool discovery endpoint
        mcpGroup.MapGet("/tools", (IMcpService mcpService) => Results.Ok(mcpService.GetTools()));

        // Remote MCP JSON-RPC execution endpoint
        mcpGroup.MapPost("", async (
            HttpContext httpContext,
            [FromBody] McpRequest request,
            [FromServices] IMcpService mcpService,
            [FromServices] IConfiguration configuration,
            CancellationToken cancellationToken) =>
        {
            if (!ValidateApiKey(httpContext, configuration))
            {
                return Results.Json(McpResponse.Fail(request.Id, -32001, "No autorizado: API Key inválida o no proporcionada."), statusCode: StatusCodes.Status401Unauthorized);
            }

            var context = CreateContext(httpContext);
            var response = await mcpService.HandleRequestAsync(request, context, cancellationToken);
            return Results.Json(response);
        });

        // -------------------------------------------------------------
        // REST Endpoints optimizados para ChatGPT Actions / OpenAPI
        // -------------------------------------------------------------
        var aiGroup = endpoints.MapGroup("/api/ai");

        // OpenAPI 3.0 Specification para ChatGPT Actions
        aiGroup.MapGet("/openapi.json", (HttpContext httpContext) =>
        {
            var hostUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            var openApiSpec = GetOpenApiSpecification(hostUrl);
            return Results.Content(openApiSpec, "application/json");
        });

        aiGroup.MapGet("/catalogs/search", async (
            HttpContext httpContext,
            [FromQuery] string catalog,
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] IEnumerable<IMcpTool> tools,
            [FromServices] IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!ValidateApiKey(httpContext, configuration)) return Results.Unauthorized();

            var tool = tools.First(t => t.Name == "search_catalogs");
            var args = JsonSerializer.SerializeToElement(new { catalog, search, page = page ?? 1, pageSize = pageSize ?? 20 });
            var result = await tool.ExecuteAsync(args, CreateContext(httpContext), ct);
            return ParseToolResult(result);
        });

        aiGroup.MapGet("/catalogs/detail", async (
            HttpContext httpContext,
            [FromQuery] string catalog,
            [FromQuery] int id,
            [FromServices] IEnumerable<IMcpTool> tools,
            [FromServices] IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!ValidateApiKey(httpContext, configuration)) return Results.Unauthorized();

            var tool = tools.First(t => t.Name == "get_catalog_detail");
            var args = JsonSerializer.SerializeToElement(new { catalog, id });
            var result = await tool.ExecuteAsync(args, CreateContext(httpContext), ct);
            return ParseToolResult(result);
        });

        aiGroup.MapGet("/contacts", async (
            HttpContext httpContext,
            [FromQuery] string? type,
            [FromQuery] string? search,
            [FromQuery] int? page,
            [FromQuery] int? pageSize,
            [FromServices] IEnumerable<IMcpTool> tools,
            [FromServices] IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!ValidateApiKey(httpContext, configuration)) return Results.Unauthorized();

            var tool = tools.First(t => t.Name == "list_contacts");
            var args = JsonSerializer.SerializeToElement(new { type = type ?? "todos", search, page = page ?? 1, pageSize = pageSize ?? 20 });
            var result = await tool.ExecuteAsync(args, CreateContext(httpContext), ct);
            return ParseToolResult(result);
        });

        aiGroup.MapGet("/adoption-requests", async (
            HttpContext httpContext,
            [FromQuery] int? processId,
            [FromQuery] string? statusFilter,
            [FromServices] IEnumerable<IMcpTool> tools,
            [FromServices] IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!ValidateApiKey(httpContext, configuration)) return Results.Unauthorized();

            var tool = tools.First(t => t.Name == "list_adoption_requests");
            var args = JsonSerializer.SerializeToElement(new { processId, statusFilter });
            var result = await tool.ExecuteAsync(args, CreateContext(httpContext), ct);
            return ParseToolResult(result);
        });

        aiGroup.MapPost("/mutate", async (
            HttpContext httpContext,
            [FromBody] JsonElement body,
            [FromServices] IEnumerable<IMcpTool> tools,
            [FromServices] IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!ValidateApiKey(httpContext, configuration)) return Results.Unauthorized();

            var tool = tools.First(t => t.Name == "mutate_entity");
            var result = await tool.ExecuteAsync(body, CreateContext(httpContext), ct);
            return ParseToolResult(result);
        });

        aiGroup.MapPost("/interpret-ocr", async (
            HttpContext httpContext,
            [FromBody] JsonElement body,
            [FromServices] IEnumerable<IMcpTool> tools,
            [FromServices] IConfiguration configuration,
            CancellationToken ct) =>
        {
            if (!ValidateApiKey(httpContext, configuration)) return Results.Unauthorized();

            var tool = tools.First(t => t.Name == "interpret_ocr_data");
            var result = await tool.ExecuteAsync(body, CreateContext(httpContext), ct);
            return ParseToolResult(result);
        });

        return endpoints;
    }

    private static bool ValidateApiKey(HttpContext httpContext, IConfiguration configuration)
    {
        var expectedKey = configuration["Mcp:ApiKey"];
        if (string.IsNullOrWhiteSpace(expectedKey))
        {
            return true; // Si no hay clave configurada en appsettings o variables de entorno, se permite (modo local/dev)
        }

        var providedKey = httpContext.Request.Headers["X-Api-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(providedKey) && httpContext.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            var authStr = authHeader.ToString();
            if (authStr.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                providedKey = authStr["Bearer ".Length..].Trim();
            }
        }

        return string.Equals(expectedKey, providedKey, StringComparison.Ordinal);
    }

    private static McpContext CreateContext(HttpContext httpContext) =>
        new(
            Caller: httpContext.User.Identity?.Name ?? "RemoteAiAgent",
            ActorUserId: Guid.Parse("00000000-0000-0000-0000-0000000000A1"),
            CorrelationId: httpContext.TraceIdentifier
        );

    private static IResult ParseToolResult(McpToolResult result)
    {
        var textContent = result.Content.FirstOrDefault()?.Text ?? "{}";
        try
        {
            using var doc = JsonDocument.Parse(textContent);
            return Results.Json(doc.RootElement.Clone(), statusCode: result.IsError ? 400 : 200);
        }
        catch
        {
            return Results.Json(new { message = textContent }, statusCode: result.IsError ? 400 : 200);
        }
    }

    private static string GetOpenApiSpecification(string hostUrl) =>
$$"""
{
  "openapi": "3.1.0",
  "info": {
    "title": "Landscape TSI AI Integration API",
    "description": "API segura para asistentes de IA (ChatGPT, Gemini, etc.) que permite consultar arquitectura de seguridad, catálogos, contactos anonimizados y procesos de adopción en Landscape TSI.",
    "version": "1.0.0"
  },
  "servers": [
    {
      "url": "{{hostUrl}}"
    }
  ],
  "paths": {
    "/api/ai/catalogs/search": {
      "get": {
        "operationId": "searchCatalogs",
        "summary": "Buscar elementos en los catálogos principales (dominio, building-block, funcionalidad, familia, tecnologia-tsi, empresa-subsidiaria, driver)",
        "parameters": [
          { "name": "catalog", "in": "query", "required": true, "schema": { "type": "string" }, "description": "Catálogo a consultar (dominio, building-block, funcionalidad, familia, tecnologia-tsi, empresa-subsidiaria, driver)" },
          { "name": "search", "in": "query", "required": false, "schema": { "type": "string" }, "description": "Texto para filtrar" },
          { "name": "page", "in": "query", "required": false, "schema": { "type": "integer", "default": 1 } },
          { "name": "pageSize", "in": "query", "required": false, "schema": { "type": "integer", "default": 20 } }
        ],
        "responses": {
          "200": { "description": "Resultados sanitizados (sin correos ni teléfonos privados)" }
        }
      }
    },
    "/api/ai/catalogs/detail": {
      "get": {
        "operationId": "getCatalogDetail",
        "summary": "Obtener el detalle de un registro específico por ID",
        "parameters": [
          { "name": "catalog", "in": "query", "required": true, "schema": { "type": "string" } },
          { "name": "id", "in": "query", "required": true, "schema": { "type": "integer" } }
        ],
        "responses": {
          "200": { "description": "Detalle del registro" }
        }
      }
    },
    "/api/ai/contacts": {
      "get": {
        "operationId": "listContacts",
        "summary": "Consultar contactos de vendors, partners, especialistas y CISOs (excluye correos y teléfonos por política de seguridad)",
        "parameters": [
          { "name": "type", "in": "query", "required": false, "schema": { "type": "string", "enum": ["vendor", "partner", "especialista_empresa", "ciso", "todos"] } },
          { "name": "search", "in": "query", "required": false, "schema": { "type": "string" } }
        ],
        "responses": {
          "200": { "description": "Lista de contactos sanitizada" }
        }
      }
    },
    "/api/ai/adoption-requests": {
      "get": {
        "operationId": "listAdoptionRequests",
        "summary": "Consultar solicitudes y procesos de adopción tecnológica TSI en curso o finalizados",
        "parameters": [
          { "name": "processId", "in": "query", "required": false, "schema": { "type": "integer" }, "description": "ID del proceso para ver el detalle de subsidiarias convocadas y tecnologías evaluadas" },
          { "name": "statusFilter", "in": "query", "required": false, "schema": { "type": "string" } }
        ],
        "responses": {
          "200": { "description": "Listado o detalle del proceso de adopción" }
        }
      }
    },
    "/api/ai/mutate": {
      "post": {
        "operationId": "mutateEntity",
        "summary": "Registrar o actualizar dominios, building blocks, funcionalidades, contactos o drivers. Requiere confirmación humana en dos pasos.",
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": {
                "type": "object",
                "properties": {
                  "entity": { "type": "string", "enum": ["dominio", "building-block", "funcionalidad", "contacto-vendor", "contacto-partner", "contacto-empresa", "driver"] },
                  "action": { "type": "string", "enum": ["create", "update"] },
                  "id": { "type": "integer" },
                  "fields": { "type": "object" },
                  "stage": { "type": "string", "enum": ["preview", "execute"], "default": "preview" },
                  "confirm": { "type": "boolean" }
                },
                "required": ["entity", "action", "fields"]
              }
            }
          }
        },
        "responses": {
          "200": { "description": "Resultado de la vista previa o confirmación de guardado" }
        }
      }
    },
    "/api/ai/interpret-ocr": {
      "post": {
        "operationId": "interpretOcrData",
        "summary": "Interpretar texto plano extraído de imagen/OCR y mapearlo a las tablas destino para posterior confirmación del usuario",
        "requestBody": {
          "required": true,
          "content": {
            "application/json": {
              "schema": {
                "type": "object",
                "properties": {
                  "rawText": { "type": "string" },
                  "targetEntitySuggestion": { "type": "string" }
                },
                "required": ["rawText"]
              }
            }
          }
        },
        "responses": {
          "200": { "description": "Campos mapeados y texto de confirmación para el usuario" }
        }
      }
    }
  }
}
""";
}
