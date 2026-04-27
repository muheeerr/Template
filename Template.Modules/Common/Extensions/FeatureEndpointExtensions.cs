using System.Reflection;
using Template.Modules.Common.Abstractions;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Template.Modules.Common.Extensions;

public static class FeatureEndpointExtensions
{
    public static IServiceCollection AddFeatureEndpoints(this IServiceCollection services, Assembly assembly)
    {
        ServiceDescriptor[] descriptors = assembly
            .DefinedTypes
            .Where(type => type is { IsAbstract: false, IsInterface: false } &&
                           typeof(IFeature).IsAssignableFrom(type))
            .Select(type => ServiceDescriptor.Transient(typeof(IFeature), type))
            .ToArray();

        services.TryAddEnumerable(descriptors);
        return services;
    }

    public static IEndpointRouteBuilder MapGroupedFeatureEndpoints(
        this IEndpointRouteBuilder endpoints,
        string openApiTag,
        Func<string, RouteGroupBuilder, RouteGroupBuilder>? configureGroup = null)
    {
        IEnumerable<IFeature> features = endpoints.ServiceProvider.GetServices<IFeature>();
        ILookup<string, IFeature> byTemplate = features
            .ToLookup(f => ResolveRouteTemplate(f.GetType()));

        foreach (IGrouping<string, IFeature> group in byTemplate)
        {
            RouteGroupBuilder routeGroup = endpoints.MapGroup($"/{group.Key}").WithTags(openApiTag);
            if (configureGroup is not null)
            {
                routeGroup = configureGroup(group.Key, routeGroup);
            }

            foreach (IFeature feature in group.OrderBy(f => f.GetType().Name, StringComparer.Ordinal))
            {
                feature.Map(routeGroup);
            }
        }

        return endpoints;
    }
    public static IApplicationBuilder MapEndpoints(this WebApplication app, RouteGroupBuilder? routeGroupBuilder = null)
    {
        IEnumerable<IFeature> features = app.Services.GetRequiredService<IEnumerable<IFeature>>();
        ILookup<string, IFeature> byTemplate = features.ToLookup(f => ResolveRouteTemplate(f.GetType()));

        IEndpointRouteBuilder builder = routeGroupBuilder is null ? app : routeGroupBuilder;
        foreach (IGrouping<string, IFeature> group in byTemplate)
        {
            string openApiTag = OpenApiTagFromTemplate(group.Key);
            RouteGroupBuilder routeGroup = builder.MapGroup($"/{group.Key}").WithTags(openApiTag);
            foreach (IFeature feature in group.OrderBy(f => f.GetType().Name, StringComparer.Ordinal))
            {
                feature.Map(routeGroup);
            }
        }

        return app;
    }

    private static string OpenApiTagFromTemplate(string template)
    {
        string[] segments = template.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (segments.Length == 0)
        {
            return template;
        }

        string last = segments[^1];
        if (last.Length == 0)
        {
            return template;
        }

        return char.ToUpperInvariant(last[0]) + last[1..];
    }
    private static string ResolveRouteTemplate(Type featureType)
    {
        foreach (Type iface in featureType.GetInterfaces().Where(i => typeof(IFeature).IsAssignableFrom(i) && i != typeof(IFeature)))
        {
            var attr = iface.GetCustomAttribute<FeatureRouteTemplateAttribute>();
            if (attr is not null)
            {
                return attr.Template;
            }
        }

        throw new InvalidOperationException(
            $"Type '{featureType.FullName}' implements {nameof(IFeature)} but no direct sub-interface carries [{nameof(FeatureRouteTemplateAttribute)}].");
    }
}
