using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Users.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Users.Features;

public sealed class UpdateUser : IUsers
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPut("/", async (UpdateUserRequest body, IMediator mediator, CancellationToken cancellationToken) =>
                (await mediator.Send(
                    new UpdateUserCommand(
                        body.Id,
                        body.FullName,
                        body.Email,
                        body.Phone,
                        body.RoleIds,
                        body.WorkingHoursStart,
                        body.WorkingHoursEnd,
                        body.Status),
                    cancellationToken)).ToApiResult("User updated successfully"))
            .RequireAuthorization();
    }

    public sealed record UpdateUserRequest(
        Guid Id,
        string FullName,
        string Email,
        string Phone,
        IReadOnlyCollection<Guid> RoleIds,
        TimeOnly WorkingHoursStart,
        TimeOnly WorkingHoursEnd,
        UserStatus Status);

    public sealed record UpdateUserCommand(
        Guid Id,
        string FullName,
        string Email,
        string Phone,
        IReadOnlyCollection<Guid> RoleIds,
        TimeOnly WorkingHoursStart,
        TimeOnly WorkingHoursEnd,
        UserStatus Status) : IAppCommand;

    public sealed class UpdateUserValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserValidator()
        {
            RuleFor(command => command.Id).NotEmpty();
            RuleFor(command => command.FullName).NotEmpty().MaximumLength(100);
            RuleFor(command => command.Email).NotEmpty().EmailAddress();
            RuleFor(command => command.Phone).NotEmpty().MaximumLength(20);
            RuleFor(command => command.RoleIds).NotEmpty();
            RuleForEach(command => command.RoleIds).NotEmpty();
        }
    }

    public sealed class UpdateUserHandler : IRequestHandler<UpdateUserCommand, AppResult>
    {
        private readonly TemplateDbContext _dbContext;

        public UpdateUserHandler(TemplateDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async ValueTask<AppResult> Handle(UpdateUserCommand command, CancellationToken cancellationToken)
        {
            var user = await _dbContext.Users.FirstOrDefaultAsync(x => x.Id == command.Id, cancellationToken);
            if (user is null)
            {
                return AppResult.Failure(Errors.NotFound("User not found."));
            }

            var distinctRoleIds = command.RoleIds.Distinct().ToArray();
            var roles = await _dbContext.Roles.AsNoTracking()
                .Where(x => distinctRoleIds.Contains(x.Id) && x.IsActive)
                .ToListAsync(cancellationToken);
            if (roles.Count != distinctRoleIds.Length)
            {
                return AppResult.Failure(
                    Errors.Validation("roleIds", "One or more roles were not found or are inactive."));
            }

            var primaryRoleId = distinctRoleIds[0];

            var duplicateEmail = await _dbContext.Users.AnyAsync(
                x => x.Email == command.Email && x.Id != command.Id,
                cancellationToken);
            if (duplicateEmail)
            {
                return AppResult.Failure(Errors.Conflict("Email already exists.", "email"));
            }

            var normalizedPhone = command.Phone.Trim();
            var duplicatePhone = await _dbContext.Users.AnyAsync(
                x => x.Phone == normalizedPhone && x.Id != command.Id,
                cancellationToken);
            if (duplicatePhone)
            {
                return AppResult.Failure(Errors.Conflict("Phone number already exists.", "phone"));
            }

            user.FullName = command.FullName.Trim();
            user.Email = command.Email.Trim().ToLowerInvariant();
            user.Phone = normalizedPhone;
            user.RoleId = primaryRoleId;
            user.WorkingHoursStart = command.WorkingHoursStart;
            user.WorkingHoursEnd = command.WorkingHoursEnd;
            user.Status = command.Status;
            user.UpdatedAt = DateTimeOffset.UtcNow;

            var requestedRoleIds = roles.Select(r => r.Id).ToHashSet();
            var existingAssignments = await _dbContext.UserRoleAssignments
                .Where(a => a.UserId == command.Id)
                .ToListAsync(cancellationToken);

            foreach (var assignment in existingAssignments)
            {
                assignment.IsActive = requestedRoleIds.Contains(assignment.RoleId);
            }

            var existingRoleIds = existingAssignments.Select(a => a.RoleId).ToHashSet();
            var assignmentNow = DateTimeOffset.UtcNow;
            foreach (var role in roles.Where(r => !existingRoleIds.Contains(r.Id)))
            {
                _dbContext.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    TenantId = user.TenantId,
                    UserId = command.Id,
                    RoleId = role.Id,
                    ScopeType = RoleAssignmentScopeType.Global,
                    IsActive = true,
                    EffectiveFrom = assignmentNow,
                    CreatedAt = assignmentNow
                });
            }

            return AppResult.Success();
        }
    }
}
