using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class ListActions : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/", async (IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(new ListActionsQuery(), cancellationToken)).ToApiResult());
    }
}
public sealed record ListActionsQuery : IAppQuery<IReadOnlyCollection<ListActionsResponse>>;

public sealed record ListActionsResponse(
    string Path,
    IReadOnlyCollection<string> Methods,
    bool RequiresAuthorization,
    bool AllowAnonymous,
    IReadOnlyCollection<string> Roles);

public sealed class ListActionsHandler : IRequestHandler<ListActionsQuery, AppResult<IReadOnlyCollection<ListActionsResponse>>>
{
    private readonly TemplateDbContext _dbContext;

    public ListActionsHandler(TemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<AppResult<IReadOnlyCollection<ListActionsResponse>>> Handle(ListActionsQuery query, CancellationToken cancellationToken)
    {
        var actions = await _dbContext.ApiActions
            .AsNoTracking()
            .Where(action => action.IsActive)
            .OrderBy(action => action.Path)
            .ToListAsync(cancellationToken);

        var assignments = await _dbContext.RoleActionAssignments
            .AsNoTracking()
            .Where(assignment => assignment.IsActive)
            .ToListAsync(cancellationToken);

        var rolesByActionId = assignments
            .GroupBy(assignment => assignment.ApiActionId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyCollection<string>)group
                    .Select(assignment => assignment.RoleCode)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(role => role)
                    .ToArray());

        var response = actions
            .Select(action => new ListActionsResponse(
                action.Path,
                action.HttpMethods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                action.RequiresAuthorization,
                action.AllowAnonymous,
                rolesByActionId.GetValueOrDefault(action.Id, [])))
            .ToArray();

        return AppResult<IReadOnlyCollection<ListActionsResponse>>.Success(response);
    }
}
