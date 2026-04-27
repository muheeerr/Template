using Template.Modules.Common.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Template.Modules.Common.Extensions;

public static class ModuleRegistrationExtensions
{
    public static IServiceCollection AddTemplateModules(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var modules = DiscoverModules();
        var endpointModules = DiscoverEndpointModules();
        services.AddSingleton<IReadOnlyCollection<ITemplateModule>>(modules);
        services.AddSingleton<IReadOnlyCollection<ITemplateEndpointModule>>(endpointModules);

        foreach (var module in modules)
        {
            module.AddServices(services, configuration);
        }

        services.AddFeatureEndpoints(typeof(ModuleRegistrationExtensions).Assembly);

        return services;
    }

    public static IEndpointRouteBuilder MapTemplateModules(this IEndpointRouteBuilder endpoints)
    {
        var modules = endpoints.ServiceProvider.GetRequiredService<IReadOnlyCollection<ITemplateModule>>();
        var endpointModules = endpoints.ServiceProvider.GetRequiredService<IReadOnlyCollection<ITemplateEndpointModule>>();

        foreach (var module in modules)
        {
            module.MapEndpoints(endpoints);
        }

        foreach (var endpointModule in endpointModules)
        {
            endpointModule.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    private static IReadOnlyCollection<ITemplateModule> DiscoverModules()
    {
        return typeof(ModuleRegistrationExtensions)
            .Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           typeof(ITemplateModule).IsAssignableFrom(type))
            .Select(type => (ITemplateModule)Activator.CreateInstance(type)!)
            .OrderBy(module => module.GetType().Name)
            .ToArray();
    }

    private static IReadOnlyCollection<ITemplateEndpointModule> DiscoverEndpointModules()
    {
        return typeof(ModuleRegistrationExtensions)
            .Assembly
            .GetTypes()
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           typeof(ITemplateEndpointModule).IsAssignableFrom(type))
            .Select(type => (ITemplateEndpointModule)Activator.CreateInstance(type)!)
            .OrderBy(module => module.GetType().Name)
            .ToArray();
    }
}
