using Template.Modules;
using Template.Modules.Modules.Auth.Domain;
using Microsoft.EntityFrameworkCore;

namespace Template.Infrastructure.Authorization;

public sealed class ActionAuthorizationService : IActionAuthorizationService
{
    private readonly TemplateDbContext _dbContext;

    public ActionAuthorizationService(TemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<string>> GetActionsForRolesAsync(
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken)
    {
        var normalizedRoles = roles
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim().ToLowerInvariant())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedRoles.Length == 0)
        {
            return [];
        }

        return await _dbContext.RoleActionAssignments
            .AsNoTracking()
            .Where(assignment => assignment.IsActive && normalizedRoles.Contains(assignment.RoleCode))
            .Join(
                _dbContext.ApiActions.AsNoTracking().Where(action => action.IsActive),
                assignment => assignment.ApiActionId,
                action => action.Id,
                (_, action) => action.Path)
            .Distinct()
            .OrderBy(path => path)
            .ToArrayAsync(cancellationToken);
    }
}
