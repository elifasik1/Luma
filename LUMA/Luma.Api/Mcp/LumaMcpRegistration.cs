using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore;
using ModelContextProtocol.Protocol;
using ModelContextProtocol.Server;

namespace Luma.Api.Mcp;

public static class LumaMcpRegistration
{
    public static IServiceCollection AddLumaMcp(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<LumaMcpTools>();

        services.AddMcpServer(options =>
        {
            options.ServerInfo = new Implementation
            {
                Name = "LUMA Alexa+ MCP Server",
                Version = "1.0.0"
            };
        })
        .WithHttpTransport(options =>
        {
            options.Stateless = true;
        })
        .WithTools<LumaMcpTools>();

        return services;
    }

    public static IEndpointConventionBuilder MapLumaMcp(this IEndpointRouteBuilder app, string pattern = "/mcp")
    {
        return app.MapMcp(pattern);
    }
}
