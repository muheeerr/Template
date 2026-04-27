using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Extensions;
using Microsoft.AspNetCore.Routing;

namespace Template.Modules.Modules.Auth;

public sealed class AuthFeatureEndpointModule : ITemplateEndpointModule
{
    public IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGroupedFeatureEndpoints(
            "Auth",
            (template, group) => template switch
            {
                "api/v1/actions" or "api/v1/roles" or "api/v1/users" => group.RequireAuthorization(),
                _ => group
            });

        return endpoints;
    }
}
