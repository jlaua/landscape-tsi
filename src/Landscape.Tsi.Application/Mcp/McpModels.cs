using System.Text.Json;
using System.Text.Json.Serialization;

namespace Landscape.Tsi.Application.Mcp;

public sealed record McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("method")]
    public string Method { get; init; } = string.Empty;

    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

public sealed record McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; init; } = "2.0";

    [JsonPropertyName("id")]
    public object? Id { get; init; }

    [JsonPropertyName("result")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Result { get; init; }

    [JsonPropertyName("error")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public McpError? Error { get; init; }

    public static McpResponse Success(object? id, object? result) =>
        new() { Id = id, Result = result };

    public static McpResponse Fail(object? id, int code, string message, object? data = null) =>
        new() { Id = id, Error = new McpError(code, message, data) };
}

public sealed record McpError(
    [property: JsonPropertyName("code")] int Code,
    [property: JsonPropertyName("message")] string Message,
    [property: JsonPropertyName("data")] object? Data = null
);

public sealed record McpToolDefinition(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("description")] string Description,
    [property: JsonPropertyName("inputSchema")] object InputSchema
);

public sealed record McpToolContent(
    [property: JsonPropertyName("type")] string Type,
    [property: JsonPropertyName("text")] string Text
);

public sealed record McpToolResult(
    [property: JsonPropertyName("content")] IReadOnlyList<McpToolContent> Content,
    [property: JsonPropertyName("isError")] bool IsError = false
)
{
    public static McpToolResult FromText(string text, bool isError = false) =>
        new([new McpToolContent("text", text)], isError);

    public static McpToolResult FromJson(object data, bool isError = false)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return new([new McpToolContent("text", JsonSerializer.Serialize(data, options))], isError);
    }
}

public sealed record McpContext(
    string Caller = "AiAgent",
    Guid? ActorUserId = null,
    string CorrelationId = ""
);
