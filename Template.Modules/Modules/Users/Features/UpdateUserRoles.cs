using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Users.Features;

public sealed class UpdateUserRoles : IUsers
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/{id:guid}/roles", async (
                Guid id,
                UpdateUserRolesRequest request,
                IMediator mediator,
                CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(
                    new UpdateUserRolesCommand(id, request.RoleIds),
                    cancellationToken);

                return result.IsSuccess ? Results.NoContent() : result.ToApiResult();
            })
            .RequireAuthorization();
    }
}

public sealed record UpdateUserRolesRequest(IReadOnlyCollection<Guid> RoleIds);

public sealed record UpdateUserRolesCommand(Guid UserId, IReadOnlyCollection<Guid> RoleIds) : IAppCommand;

public sealed class UpdateUserRolesValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesValidator()
    {
        RuleFor(command => command.UserId).NotEmpty();
        RuleForEach(command => command.RoleIds).NotEmpty();
    }
}

public sealed class UpdateUserRolesHandler : IRequestHandler<UpdateUserRolesCommand, AppResult>
{
    private readonly TemplateDbContext _dbContext;

    public UpdateUserRolesHandler(TemplateDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async ValueTask<AppResult> Handle(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(e => e.Id == command.UserId, cancellationToken);
        if (user is null)
        {
            return AppResult.Failure(Errors.NotFound("User not found."));
        }

        var requestedRoleIdsInput = (command.RoleIds ?? Array.Empty<Guid>())
            .Where(id => id != Guid.Empty)
            .Distinct()
            .ToArray();

        var matchedRoles = requestedRoleIdsInput.Length == 0
            ? []
            : await _dbContext.Roles
                .Where(role => role.IsActive && requestedRoleIdsInput.Contains(role.Id))
                .ToListAsync(cancellationToken);

        var existingAssignments = await _dbContext.UserRoleAssignments
            .Where(assignment => assignment.UserId == command.UserId)
            .ToListAsync(cancellationToken);

        var requestedRoleIds = matchedRoles
            .Select(role => role.Id)
            .ToHashSet();

        foreach (var assignment in existingAssignments)
        {
            assignment.IsActive = requestedRoleIds.Contains(assignment.RoleId);
        }

        var existingRoleIds = existingAssignments
            .Select(assignment => assignment.RoleId)
            .ToHashSet();

        var utcNow = DateTimeOffset.UtcNow;
        foreach (var role in matchedRoles.Where(role => !existingRoleIds.Contains(role.Id)))
        {
            _dbContext.UserRoleAssignments.Add(new UserRoleAssignment
            {
                Id = Guid.NewGuid(),
                TenantId = user.TenantId,
                UserId = command.UserId,
                RoleId = role.Id,
                ScopeType = RoleAssignmentScopeType.Global,
                IsActive = true,
                EffectiveFrom = utcNow,
                CreatedAt = utcNow
            });
        }

        return AppResult.Success();
    }
}
