using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class GetRoles : IRoles
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
                (await mediator.Send(new ListRolesQuery(), cancellationToken)).ToApiResult())
            .RequireAuthorization();
    }
}
public sealed record ListRolesQuery : IAppQuery<IReadOnlyCollection<ListRolesResponse>>;

public sealed record ListRolesResponse(Guid Id, string Code, string Name, string? Description, bool IsSystem, bool IsActive);

public sealed class ListRolesHandler : IRequestHandler<ListRolesQuery, AppResult<IReadOnlyCollection<ListRolesResponse>>>
{
    private readonly TemplateDbContext _dbContext;

    public ListRolesHandler(TemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<AppResult<IReadOnlyCollection<ListRolesResponse>>> Handle(ListRolesQuery query, CancellationToken cancellationToken)
    {
        var roles = await _dbContext.Roles
            .AsNoTracking()
            .OrderBy(role => role.Code)
            .Select(role => new ListRolesResponse(role.Id, role.Code, role.Name, role.Description, role.IsSystem, role.IsActive))
            .ToListAsync(cancellationToken);

        return AppResult<IReadOnlyCollection<ListRolesResponse>>.Success(roles);
    }
}
