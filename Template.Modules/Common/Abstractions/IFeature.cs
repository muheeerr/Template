using Microsoft.AspNetCore.Routing;

namespace Template.Modules.Common.Abstractions;

public interface IFeature
{
    void Map(IEndpointRouteBuilder app);
}
