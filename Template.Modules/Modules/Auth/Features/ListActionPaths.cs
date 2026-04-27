using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class GetActionPaths : IActions
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/paths", async (IMediator mediator, CancellationToken cancellationToken) =>
                (await mediator.Send(new ListActionPathsQuery(), cancellationToken)).ToApiResult())
            .RequireAuthorization();
    }
}

public sealed record ListActionPathsQuery : IAppQuery<IReadOnlyCollection<ActionPathItem>>;

public sealed record ActionPathItem(
    string Path,
    IReadOnlyCollection<string> HttpMethods,
    bool RequiresAuthorization,
    bool AllowAnonymous,
    bool IsActive);

public sealed class ListActionPathsHandler : IRequestHandler<ListActionPathsQuery, AppResult<IReadOnlyCollection<ActionPathItem>>>
{
    private readonly TemplateDbContext _dbContext;
    private readonly IApiActionCatalogSynchronizer _catalogSynchronizer;

    public ListActionPathsHandler(TemplateDbContext dbContext, IApiActionCatalogSynchronizer catalogSynchronizer)
    {
        _dbContext = dbContext;
        _catalogSynchronizer = catalogSynchronizer;
    }

    public async ValueTask<AppResult<IReadOnlyCollection<ActionPathItem>>> Handle(ListActionPathsQuery query, CancellationToken cancellationToken)
    {
        await _catalogSynchronizer.SyncAsync(cancellationToken);

        var actions = await _dbContext.ApiActions
            .AsNoTracking()
            .Where(action => action.IsActive)
            .OrderBy(action => action.Path)
            .ToListAsync(cancellationToken);

        var items = actions
            .Select(action => new ActionPathItem(
                action.Path,
                action.HttpMethods.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
                action.RequiresAuthorization,
                action.AllowAnonymous,
                action.IsActive))
            .ToArray();

        return AppResult<IReadOnlyCollection<ActionPathItem>>.Success(items);
    }
}
