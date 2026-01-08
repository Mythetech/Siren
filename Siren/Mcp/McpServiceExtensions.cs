using Microsoft.Extensions.DependencyInjection;
using Mythetech.Framework.Infrastructure.Mcp;

namespace Siren.Mcp;

public static class McpServiceExtensions
{
    public static IServiceCollection AddSirenMcp(this IServiceCollection services)
    {
        services.AddMcp();
        services.AddMcpTools(typeof(McpServiceExtensions).Assembly);
        return services;
    }

    public static IServiceProvider UseSirenMcp(this IServiceProvider provider)
    {
        provider.UseMcp(typeof(McpServiceExtensions).Assembly);
        return provider;
    }
}
