using Microsoft.AspNetCore.Routing;

namespace Template.Modules.Common.Abstractions;

public interface ITemplateEndpointModule
{
    IEndpointRouteBuilder MapEndpoints(IEndpointRouteBuilder endpoints);
}
