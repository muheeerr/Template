using Template.Modules.Common.Authorization;
using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class UpdateRoleActionAssignments : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/{roleId:guid}/actions", async (
                Guid roleId,
                UpdateRoleActionAssignmentsRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(
                new UpdateRoleActionAssignmentsCommand(roleId, request.ActionPaths),
                cancellationToken);

            return result.ToApiResult("The requested actions were added to the role; existing assignments are unchanged.");
        });
    }
}

public sealed record UpdateRoleActionAssignmentsRequest(IReadOnlyCollection<string> ActionPaths);

public sealed record UpdateRoleActionAssignmentsCommand(Guid RoleId, IReadOnlyCollection<string> ActionPaths) : IAppCommand;

public sealed class UpdateRoleActionAssignmentsValidator : AbstractValidator<UpdateRoleActionAssignmentsCommand>
{
    public UpdateRoleActionAssignmentsValidator()
    {
        RuleFor(command => command.RoleId).NotEmpty();
        RuleFor(command => command.ActionPaths)
            .Must(paths => paths == null || paths.All(p => !string.IsNullOrWhiteSpace(p)))
            .WithMessage("Each action path must be non-empty.");
    }
}

public sealed class UpdateRoleActionAssignmentsHandler : IRequestHandler<UpdateRoleActionAssignmentsCommand, AppResult>
{
    private readonly TemplateDbContext _dbContext;

    public UpdateRoleActionAssignmentsHandler(TemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<AppResult> Handle(UpdateRoleActionAssignmentsCommand command, CancellationToken cancellationToken)
    {
        var role = await _dbContext.Roles
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == command.RoleId, cancellationToken);
        if (role is null)
        {
            return AppResult.Failure(Errors.NotFound("Role was not found."));
        }

        var normalizedRoleCode = role.Code.Trim().ToLowerInvariant();
        var requestedPaths = (command.ActionPaths ?? [])
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(ApiActionPath.Normalize)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var matchedActions = requestedPaths.Length == 0
            ? []
            : await _dbContext.ApiActions
                .Where(action => action.IsActive && requestedPaths.Contains(action.Path))
                .ToListAsync(cancellationToken);

        var missingPaths = requestedPaths
            .Except(matchedActions.Select(action => action.Path), StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (missingPaths.Length > 0)
        {
            return AppResult.Failure(Errors.NotFound($"The following actions were not found: {string.Join(", ", missingPaths)}"));
        }

        // Additive: payload actions are granted; other role assignments stay as they are.
        // IgnoreQueryFilters so we can reactivate a row that was turned off for this role without inserting a duplicate.
        var existingAssignments = await _dbContext.RoleActionAssignments
            .IgnoreQueryFilters()
            .Where(assignment => assignment.RoleCode == normalizedRoleCode)
            .ToListAsync(cancellationToken);

        var requestedActionIds = matchedActions
            .Select(action => action.Id)
            .ToHashSet();

        foreach (var assignment in existingAssignments.Where(a => requestedActionIds.Contains(a.ApiActionId)))
        {
            assignment.IsActive = true;
            assignment.IsDeleted = false;
        }

        var existingActionIds = existingAssignments
            .Select(assignment => assignment.ApiActionId)
            .ToHashSet();

        var actionsToAdd = matchedActions.Where(action => !existingActionIds.Contains(action.Id));
        foreach (var action in actionsToAdd)
        {
            _dbContext.RoleActionAssignments.Add(new RoleActionAssignment
            {
                Id = Guid.NewGuid(),
                RoleCode = normalizedRoleCode,
                ApiActionId = action.Id,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        return AppResult.Success();
    }
}
