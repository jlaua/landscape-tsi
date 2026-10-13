using System.Text.Json;

namespace Landscape.Tsi.Application.Mcp;

public sealed class McpService : IMcpService
{
    private readonly IEnumerable<IMcpTool> _tools;

    public McpService(IEnumerable<IMcpTool> tools)
    {
        _tools = tools;
    }

    public IReadOnlyList<McpToolDefinition> GetTools()
    {
        return _tools.Select(t => new McpToolDefinition(
            t.Name,
            t.Description,
            t.InputSchema
        )).ToList();
    }

    public async Task<McpResponse> HandleRequestAsync(McpRequest request, McpContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            switch (request.Method)
            {
                case "initialize":
                    return McpResponse.Success(request.Id, new
                    {
                        protocolVersion = "2024-11-05",
                        serverInfo = new
                        {
                            name = "Landscape.Tsi.McpServer",
                            version = "1.0.0"
                        },
                        capabilities = new
                        {
                            tools = new { }
                        }
                    });

                case "notifications/initialized":
                    return McpResponse.Success(request.Id, new { status = "ready" });

                case "ping":
                    return McpResponse.Success(request.Id, new { });

                case "tools/list":
                    return McpResponse.Success(request.Id, new
                    {
                        tools = GetTools()
                    });

                case "tools/call":
                    if (!request.Params.HasValue)
                    {
                        return McpResponse.Fail(request.Id, -32602, "Invalid params: params object is missing.");
                    }

                    var paramsElem = request.Params.Value;
                    if (!paramsElem.TryGetProperty("name", out var toolNameProp) || toolNameProp.GetString() is not { } toolName)
                    {
                        return McpResponse.Fail(request.Id, -32602, "Invalid params: 'name' is required.");
                    }

                    var tool = _tools.FirstOrDefault(t => t.Name.Equals(toolName, StringComparison.OrdinalIgnoreCase));
                    if (tool is null)
                    {
                        return McpResponse.Fail(request.Id, -32601, $"Tool '{toolName}' not found.");
                    }

                    var args = paramsElem.TryGetProperty("arguments", out var argsProp) && argsProp.ValueKind == JsonValueKind.Object
                        ? argsProp
                        : JsonDocument.Parse("{}").RootElement;

                    var result = await tool.ExecuteAsync(args, context, cancellationToken);
                    return McpResponse.Success(request.Id, result);

                default:
                    return McpResponse.Fail(request.Id, -32601, $"Method '{request.Method}' not found.");
            }
        }
        catch (Exception ex)
        {
            return McpResponse.Fail(request.Id, -32603, $"Internal error: {ex.Message}");
        }
    }
}
