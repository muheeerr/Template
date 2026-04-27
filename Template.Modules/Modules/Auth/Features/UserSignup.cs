using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Users.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class UserSignup : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/signup", async (UserSignupCommand command, IMediator mediator, CancellationToken cancellationToken) =>
                (await mediator.Send(command, cancellationToken)).ToApiResult("Signup successful"))
            .AllowAnonymous();
    }

    public sealed record UserSignupCommand(string TenantSlug, string Email, string Password) : IAppCommand<AuthResponse>;

    public sealed class UserSignupValidator : AbstractValidator<UserSignupCommand>
    {
        public UserSignupValidator()
        {
            RuleFor(command => command.TenantSlug).NotEmpty().MaximumLength(100);
            RuleFor(command => command.Email).NotEmpty().EmailAddress();
            RuleFor(command => command.Password).NotEmpty().MinimumLength(8);
        }
    }

    public sealed class UserSignupHandler : IRequestHandler<UserSignupCommand, AppResult<AuthResponse>>
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ICredentialHasher _credentialHasher;
        private readonly ITokenService _tokenService;
        private readonly IActionAuthorizationService _actionAuthorizationService;

        public UserSignupHandler(
            TemplateDbContext dbContext,
            ICredentialHasher credentialHasher,
            ITokenService tokenService,
            IActionAuthorizationService actionAuthorizationService)
        {
            _dbContext = dbContext;
            _credentialHasher = credentialHasher;
            _tokenService = tokenService;
            _actionAuthorizationService = actionAuthorizationService;
        }

        public async ValueTask<AppResult<AuthResponse>> Handle(UserSignupCommand command, CancellationToken cancellationToken)
        {
            var tenantSlug = command.TenantSlug.Trim().ToLowerInvariant();
            var tenant = await _dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Slug == tenantSlug && !t.IsDeleted, cancellationToken);
            if (tenant is null || !tenant.IsActive)
            {
                return AppResult<AuthResponse>.Failure(Errors.NotFound("Unknown or inactive tenant."));
            }

            var normalizedEmail = command.Email.Trim().ToLowerInvariant();
            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    e => e.TenantId == tenant.Id && e.Email == normalizedEmail && !e.IsDeleted,
                    cancellationToken);

            if (user is null)
            {
                return AppResult<AuthResponse>.Failure(
                    Errors.NotFound("No user profile was found for this email address."));
            }

            if (!user.IsActive || user.Status == UserStatus.Inactive)
            {
                return AppResult<AuthResponse>.Failure(Errors.Forbidden("The user account is inactive."));
            }

            var existingAuth = await _dbContext.AuthAccounts
                .IgnoreQueryFilters()
                .AnyAsync(a => a.TenantId == tenant.Id && a.UserId == user.Id, cancellationToken);
            if (existingAuth)
            {
                return AppResult<AuthResponse>.Failure(
                    Errors.Conflict("An account is already registered for this user."));
            }

            if (await _dbContext.AuthAccounts.IgnoreQueryFilters()
                    .AnyAsync(a => a.TenantId == tenant.Id && a.Username == normalizedEmail, cancellationToken))
            {
                return AppResult<AuthResponse>.Failure(Errors.Conflict("Username already exists.", "email"));
            }

            var roles = await _dbContext.UserRoleAssignments
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenant.Id && x.UserId == user.Id && x.IsActive && !x.IsDeleted)
                .Join(
                    _dbContext.Roles.IgnoreQueryFilters().Where(r => r.TenantId == tenant.Id && !r.IsDeleted),
                    era => era.RoleId,
                    r => r.Id,
                    (_, r) => r.Code)
                .ToArrayAsync(cancellationToken);

            if (roles.Length == 0)
            {
                return AppResult<AuthResponse>.Failure(
                    Errors.Forbidden(
                        "No roles are assigned to this user. Sign-up is not available until roles are assigned when the user is created."));
            }

            var account = new AuthAccount
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                Username = normalizedEmail,
                PasswordHash = _credentialHasher.Hash(command.Password),
                AuthProvider = AuthProvider.Local,
                MustChangePassword = false,
                CreatedAt = DateTimeOffset.UtcNow,
                UpdatedAt = DateTimeOffset.UtcNow
            };

            _dbContext.AuthAccounts.Add(account);

            var actions = await _actionAuthorizationService.GetActionsForRolesAsync(roles, cancellationToken);

            account.LastLoginAt = DateTimeOffset.UtcNow;
            account.UpdatedAt = DateTimeOffset.UtcNow;

            var accessToken = _tokenService.CreateAccessToken(new AuthenticatedPrincipal(
                tenant.Id,
                user.Id,
                user.Email,
                user.FullName,
                roles,
                actions,
                account.TokenVersion));
            var refreshToken = _tokenService.CreateOpaqueToken();

            _dbContext.UserSessions.Add(new UserSession
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                UserId = user.Id,
                RefreshTokenHash = _credentialHasher.Hash(refreshToken),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
                CreatedAt = DateTimeOffset.UtcNow
            });

            return AppResult<AuthResponse>.Success(
                AuthDtoFactory.ToAuthResponse(user, tenant.Slug, roles, actions, accessToken, refreshToken, account.MustChangePassword));
        }
    }
}
