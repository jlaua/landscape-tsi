using Landscape.Tsi.Application.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace Landscape.Tsi.Application.Mcp;

public static class DependencyInjection
{
    public static IServiceCollection AddLandscapeMcp(this IServiceCollection services)
    {
        services.AddScoped<IMcpTool, SearchCatalogsTool>();
        services.AddScoped<IMcpTool, GetCatalogDetailTool>();
        services.AddScoped<IMcpTool, ListContactsTool>();
        services.AddScoped<IMcpTool, ListAdoptionRequestsTool>();
        services.AddScoped<IMcpTool, EntityMutationTool>();
        services.AddScoped<IMcpTool, InterpretOcrDataTool>();
        services.AddScoped<IMcpService, McpService>();
        return services;
    }
}
