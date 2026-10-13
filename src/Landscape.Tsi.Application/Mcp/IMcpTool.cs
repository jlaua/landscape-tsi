using System.Text.Json;

namespace Landscape.Tsi.Application.Mcp;

public interface IMcpTool
{
    string Name { get; }
    string Description { get; }
    object InputSchema { get; }
    Task<McpToolResult> ExecuteAsync(JsonElement arguments, McpContext context, CancellationToken cancellationToken = default);
}

public interface IMcpService
{
    IReadOnlyList<McpToolDefinition> GetTools();
    Task<McpResponse> HandleRequestAsync(McpRequest request, McpContext context, CancellationToken cancellationToken = default);
}
