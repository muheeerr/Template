using Template.Modules;
using Template.Modules.Common.Authorization;
using Template.Modules.Modules.Auth.Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Template.Infrastructure.Authorization;

public sealed class ApiActionCatalogSynchronizer : IApiActionCatalogSynchronizer
{
    private readonly IEnumerable<EndpointDataSource> _endpointDataSources;
    private readonly TemplateDbContext _dbContext;
    private readonly ILogger<ApiActionCatalogSynchronizer> _logger;

    public ApiActionCatalogSynchronizer(
        IEnumerable<EndpointDataSource> endpointDataSources,
        TemplateDbContext dbContext,
        ILogger<ApiActionCatalogSynchronizer> logger)
    {
        _endpointDataSources = endpointDataSources;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken cancellationToken)
    {
        var discoveredActions = DiscoverApiActions();
        var discoveredByPath = discoveredActions.ToDictionary(action => action.Path, StringComparer.OrdinalIgnoreCase);
        // Include inactive / soft-deleted rows: IX_api_actions_path is unfiltered, so inserts would collide otherwise.
        var existingActions = await _dbContext.ApiActions
            .IgnoreQueryFilters()
            .ToListAsync(cancellationToken);
        var utcNow = DateTimeOffset.UtcNow;

        foreach (var action in existingActions)
        {
            if (!discoveredByPath.TryGetValue(action.Path, out var discovered))
            {
                action.IsActive = false;
                action.UpdatedAt = utcNow;
                continue;
            }

            action.HttpMethods = string.Join(',', discovered.HttpMethods);
            action.RequiresAuthorization = discovered.RequiresAuthorization;
            action.AllowAnonymous = discovered.AllowAnonymous;
            action.IsActive = true;
            action.IsDeleted = false;
            action.UpdatedAt = utcNow;
        }

        var existingPaths = existingActions
            .Select(action => action.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var discovered in discoveredActions.Where(action => !existingPaths.Contains(action.Path)))
        {
            _dbContext.ApiActions.Add(new ApiAction
            {
                Id = Guid.NewGuid(),
                Path = discovered.Path,
                HttpMethods = string.Join(',', discovered.HttpMethods),
                RequiresAuthorization = discovered.RequiresAuthorization,
                AllowAnonymous = discovered.AllowAnonymous,
                IsActive = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        await EnsureAdministratorAssignmentsAsync(cancellationToken);

        _logger.LogInformation("Synchronized {ActionCount} API actions into the authorization catalog", discoveredActions.Count);
    }

    private async Task EnsureAdministratorAssignmentsAsync(CancellationToken cancellationToken)
    {
        var administratorActions = await _dbContext.ApiActions
            .Where(action => action.IsActive && action.RequiresAuthorization && !action.AllowAnonymous)
            .Select(action => action.Id)
            .ToListAsync(cancellationToken);

        if (administratorActions.Count == 0)
        {
            return;
        }

        var existingAssignments = await _dbContext.RoleActionAssignments
            .Where(assignment => assignment.RoleCode == AppRoles.Administrator)
            .ToListAsync(cancellationToken);

        var assignmentLookup = existingAssignments.ToDictionary(assignment => assignment.ApiActionId);
        foreach (var actionId in administratorActions)
        {
            if (assignmentLookup.TryGetValue(actionId, out var existingAssignment))
            {
                existingAssignment.IsActive = true;
                continue;
            }

            _dbContext.RoleActionAssignments.Add(new RoleActionAssignment
            {
                Id = Guid.NewGuid(),
                RoleCode = AppRoles.Administrator,
                ApiActionId = actionId,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private IReadOnlyCollection<DiscoveredApiAction> DiscoverApiActions()
    {
        return _endpointDataSources
            .SelectMany(dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(ToDiscoveredApiAction)
            .Where(action => action is not null)
            .GroupBy(action => action!.Path, StringComparer.OrdinalIgnoreCase)
            .Select(group => new DiscoveredApiAction(
                group.Key,
                group.SelectMany(action => action!.HttpMethods)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Order(StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                group.Any(action => action!.RequiresAuthorization),
                group.All(action => action!.AllowAnonymous)))
            .OrderBy(action => action.Path, StringComparer.OrdinalIgnoreCase)
            .ToArray()!;
    }

    private static DiscoveredApiAction? ToDiscoveredApiAction(RouteEndpoint endpoint)
    {
        var path = ApiActionPath.FromEndpoint(endpoint);
        if (path is null || !ApiActionPath.IsApiPath(path))
        {
            return null;
        }

        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToArray() ?? ["GET"];

        return new DiscoveredApiAction(
            path,
            methods,
            endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count > 0,
            endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null);
    }

    private sealed record DiscoveredApiAction(
        string Path,
        IReadOnlyCollection<string> HttpMethods,
        bool RequiresAuthorization,
        bool AllowAnonymous);
}
