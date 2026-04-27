using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Users.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class Login : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/login", async (LoginCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(command, cancellationToken)).ToApiResult("Login successful"))
            .AllowAnonymous();
    }


    public sealed record LoginCommand(string TenantSlug, string Email, string Password) : IAppCommand<AuthResponse>;

    public sealed class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(command => command.TenantSlug).NotEmpty().MaximumLength(100);
            RuleFor(command => command.Email).NotEmpty().EmailAddress();
            RuleFor(command => command.Password).NotEmpty();
        }
    }

    public sealed class LoginHandler : IRequestHandler<LoginCommand, AppResult<AuthResponse>>
    {
        private readonly TemplateDbContext _dbContext;
        private readonly ICredentialHasher _credentialHasher;
        private readonly ITokenService _tokenService;
        private readonly IActionAuthorizationService _actionAuthorizationService;

        public LoginHandler(
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

        public async ValueTask<AppResult<AuthResponse>> Handle(LoginCommand command, CancellationToken cancellationToken)
        {
            var tenantSlug = command.TenantSlug.Trim().ToLowerInvariant();
            var tenant = await _dbContext.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    t => t.Slug == tenantSlug && !t.IsDeleted,
                    cancellationToken);
            if (tenant is null || !tenant.IsActive)
            {
                return AppResult<AuthResponse>.Failure(Errors.Unauthorized("Unknown or inactive tenant."));
            }

            var normalizedEmail = command.Email.Trim().ToLowerInvariant();
            var account = await _dbContext.AuthAccounts
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(
                    x => x.TenantId == tenant.Id && x.Username == normalizedEmail,
                    cancellationToken);
            if (account is null || string.IsNullOrWhiteSpace(account.PasswordHash) || account.IsLocked)
            {
                return AppResult<AuthResponse>.Failure(Errors.Unauthorized("Invalid email or password."));
            }

            if (!_credentialHasher.Verify(account.PasswordHash, command.Password))
            {
                return AppResult<AuthResponse>.Failure(Errors.Unauthorized("Invalid email or password."));
            }

            var user = await _dbContext.Users
                .IgnoreQueryFilters()
                .FirstAsync(x => x.Id == account.UserId && x.TenantId == tenant.Id, cancellationToken);
            if (!user.IsActive || user.Status == UserStatus.Inactive)
            {
                return AppResult<AuthResponse>.Failure(Errors.Forbidden("The user account is inactive."));
            }

            var roles = await _dbContext.UserRoleAssignments
                .IgnoreQueryFilters()
                .Where(x => x.TenantId == tenant.Id && x.UserId == user.Id && x.IsActive && !x.IsDeleted)
                .Join(
                    _dbContext.Roles.IgnoreQueryFilters().Where(r => r.TenantId == tenant.Id && !r.IsDeleted),
                    era => era.RoleId,
                    role => role.Id,
                    (_, role) => role.Code)
                .ToArrayAsync(cancellationToken);
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