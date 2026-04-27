using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Users.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using NanoidDotNet;

namespace Template.Modules.Modules.Users.Features;

public sealed class CreateUser : IUsers
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/", async (CreateUserCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            {
                var result = await mediator.Send(command, cancellationToken);
                return result.IsSuccess
                    ? Results.Created($"/api/v1/users/{result.Value!.Id}", result.Value)
                    : result.ToApiResult();
            })
            .RequireAuthorization();
    }

    public sealed record CreateUserCommand(
        string FullName,
        string Email,
        string Phone,
        IReadOnlyCollection<Guid> RoleIds,
        UserStatus? Status) : IAppCommand<CreateUserResponse>;

    public sealed record CreateUserResponse(
        Guid Id,
        string UserCode,
        string FullName,
        string Email,
        string Phone,
        Guid RoleId,
        string RoleName);

    public sealed class CreateUserValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserValidator()
        {
            RuleFor(command => command.FullName).NotEmpty().MaximumLength(100);
            RuleFor(command => command.Email).NotEmpty().EmailAddress();
            RuleFor(command => command.Phone).NotEmpty().MaximumLength(20);
            RuleFor(command => command.RoleIds).NotEmpty();
            RuleForEach(command => command.RoleIds).NotEmpty();
        }
    }

    public sealed class CreateUserHandler : IRequestHandler<CreateUserCommand, AppResult<CreateUserResponse>>
    {
        private const string TemporaryPasswordAlphabet = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";

        private readonly TemplateDbContext _dbContext;
        private readonly ICredentialHasher _credentialHasher;
        private readonly IUserOnboardingEmailDispatch _onboardingEmailDispatch;
        private readonly ICurrentTenant _currentTenant;

        public CreateUserHandler(
            TemplateDbContext dbContext,
            ICredentialHasher credentialHasher,
            IUserOnboardingEmailDispatch onboardingEmailDispatch,
            ICurrentTenant currentTenant)
        {
            _dbContext = dbContext;
            _credentialHasher = credentialHasher;
            _onboardingEmailDispatch = onboardingEmailDispatch;
            _currentTenant = currentTenant;
        }

        public async ValueTask<AppResult<CreateUserResponse>> Handle(CreateUserCommand command, CancellationToken cancellationToken)
        {
            if (_currentTenant.TenantId is not { } tenantId)
            {
                return AppResult<CreateUserResponse>.Failure(Errors.Unauthorized());
            }

            var distinctRoleIds = command.RoleIds.Distinct().ToArray();
            var roles = await _dbContext.Roles.AsNoTracking()
                .Where(x => distinctRoleIds.Contains(x.Id) && x.IsActive)
                .ToListAsync(cancellationToken);
            if (roles.Count != distinctRoleIds.Length)
            {
                return AppResult<CreateUserResponse>.Failure(
                    Errors.Validation("roleIds", "One or more roles were not found or are inactive."));
            }

            var primaryRoleId = distinctRoleIds[0];
            var primaryRole = roles.First(x => x.Id == primaryRoleId);

            var normalizedEmail = command.Email.Trim().ToLowerInvariant();
            var emailExists = await _dbContext.Users.AnyAsync(x => x.Email == normalizedEmail, cancellationToken);
            if (emailExists)
            {
                return AppResult<CreateUserResponse>.Failure(Errors.Conflict("Email already exists.", "email"));
            }

            var usernameTaken = await _dbContext.AuthAccounts.AnyAsync(x => x.Username == normalizedEmail, cancellationToken);
            if (usernameTaken)
            {
                return AppResult<CreateUserResponse>.Failure(
                    Errors.Conflict("This email is already used for a login account.", "email"));
            }

            var normalizedPhone = command.Phone.Trim();
            var phoneExists = await _dbContext.Users.AnyAsync(x => x.Phone == normalizedPhone, cancellationToken);
            if (phoneExists)
            {
                return AppResult<CreateUserResponse>.Failure(Errors.Conflict("Phone number already exists.", "phone"));
            }

            var nextNumber = await _dbContext.Users.CountAsync(cancellationToken) + 1;
            var user = new User
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserCode = $"usr{nextNumber}",
                FullName = command.FullName.Trim(),
                Email = normalizedEmail,
                Phone = normalizedPhone,
                RoleId = primaryRoleId,
                WorkingHoursStart = new TimeOnly(9, 0),
                WorkingHoursEnd = new TimeOnly(18, 0),
                Status = command.Status ?? UserStatus.Active,
                IsActive = true,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.Users.Add(user);

            var assignmentNow = DateTimeOffset.UtcNow;
            foreach (var roleId in distinctRoleIds)
            {
                _dbContext.UserRoleAssignments.Add(new UserRoleAssignment
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenantId,
                    UserId = user.Id,
                    RoleId = roleId,
                    ScopeType = RoleAssignmentScopeType.Global,
                    IsActive = true,
                    EffectiveFrom = assignmentNow,
                    CreatedAt = assignmentNow
                });
            }

            var temporaryPassword = Nanoid.Generate(TemporaryPasswordAlphabet, 8);
            _dbContext.AuthAccounts.Add(new AuthAccount
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = user.Id,
                Username = normalizedEmail,
                PasswordHash = _credentialHasher.Hash(temporaryPassword),
                AuthProvider = AuthProvider.Local,
                MustChangePassword = true,
                TotpSecret = null,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            });

            _onboardingEmailDispatch.ScheduleWelcome(normalizedEmail, user.FullName, temporaryPassword);

            return AppResult<CreateUserResponse>.Success(
                new CreateUserResponse(
                    user.Id,
                    user.UserCode,
                    user.FullName,
                    user.Email,
                    user.Phone,
                    user.RoleId,
                    primaryRole.Name));
        }
    }
}
