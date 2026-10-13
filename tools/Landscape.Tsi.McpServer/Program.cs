using System.Text.Json;
using Landscape.Tsi.Application.Mcp;
using Landscape.Tsi.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);
builder.Logging.ClearProviders();

builder.Configuration
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json", optional: true)
    .AddUserSecrets("landscape-tsi-web-development")
    .AddEnvironmentVariables();

builder.Services.AddIdentityInfrastructure(builder.Configuration);
builder.Services.AddLandscapeMcp();

var host = builder.Build();
await host.Services.InitializeIdentityInfrastructureAsync();

var mcpService = host.Services.GetRequiredService<IMcpService>();
var mcpContext = new McpContext("LocalAdminCli", Guid.Parse("00000000-0000-0000-0000-0000000000A1"), Guid.NewGuid().ToString("N"));

using var reader = Console.In;
while (await reader.ReadLineAsync() is { } line)
{
    if (string.IsNullOrWhiteSpace(line))
    {
        continue;
    }

    try
    {
        var request = JsonSerializer.Deserialize<McpRequest>(line);
        if (request is null || request.Id is null)
        {
            continue;
        }

        var response = await mcpService.HandleRequestAsync(request, mcpContext);
        var jsonResponse = JsonSerializer.Serialize(response);
        Console.WriteLine(jsonResponse);
        await Console.Out.FlushAsync();
    }
    catch (Exception ex)
    {
        var errResponse = McpResponse.Fail(null, -32700, $"Parse error: {ex.Message}");
        Console.WriteLine(JsonSerializer.Serialize(errResponse));
        await Console.Out.FlushAsync();
    }
}
