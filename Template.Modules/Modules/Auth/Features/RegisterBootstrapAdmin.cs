using Template.Modules.Common.Authorization;
using Template.Modules.Persistence;
using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Users.Domain;
using FluentValidation;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Template.Modules.Modules.Auth.Features;

public sealed class RegisterBootstrapAdmin : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/register", async (RegisterBootstrapAdminCommand command, IMediator mediator, CancellationToken cancellationToken) =>
        {
            var result = await mediator.Send(command, cancellationToken);
            return result.IsSuccess
                ? Results.Created("/api/v1/auth/me", result.Value)
                : result.ToApiResult();
        }).AllowAnonymous();
    }
}
public sealed record RegisterBootstrapAdminCommand(string FullName, string Email, string Password, string ConfirmPassword) : IAppCommand<AuthResponse>;

public sealed class RegisterBootstrapAdminValidator : AbstractValidator<RegisterBootstrapAdminCommand>
{
    public RegisterBootstrapAdminValidator()
    {
        RuleFor(command => command.FullName).NotEmpty().MaximumLength(100);
        RuleFor(command => command.Email).NotEmpty().EmailAddress();
        RuleFor(command => command.Password).NotEmpty().MinimumLength(8);
        RuleFor(command => command.ConfirmPassword).Equal(command => command.Password);
    }
}

public sealed class RegisterBootstrapAdminHandler : IRequestHandler<RegisterBootstrapAdminCommand, AppResult<AuthResponse>>
{
    private readonly TemplateDbContext _dbContext;
    private readonly ICredentialHasher _credentialHasher;
    private readonly ITokenService _tokenService;
    private readonly IActionAuthorizationService _actionAuthorizationService;
    private readonly IOptions<BootstrapOptions> _bootstrapOptions;

    public RegisterBootstrapAdminHandler(
        TemplateDbContext dbContext,
        ICredentialHasher credentialHasher,
        ITokenService tokenService,
        IActionAuthorizationService actionAuthorizationService,
        IOptions<BootstrapOptions> bootstrapOptions)
    {
        _dbContext = dbContext;
        _credentialHasher = credentialHasher;
        _tokenService = tokenService;
        _actionAuthorizationService = actionAuthorizationService;
        _bootstrapOptions = bootstrapOptions;
    }

    public async ValueTask<AppResult<AuthResponse>> Handle(RegisterBootstrapAdminCommand command, CancellationToken cancellationToken)
    {
        if (!_bootstrapOptions.Value.Enabled)
        {
            return AppResult<AuthResponse>.Failure(Errors.Forbidden("Bootstrap registration is disabled."));
        }

        var authAccountExists = await _dbContext.AuthAccounts.IgnoreQueryFilters().AnyAsync(cancellationToken);
        if (authAccountExists)
        {
            return AppResult<AuthResponse>.Failure(Errors.Forbidden("Bootstrap registration is only available before the first account exists."));
        }

        var tenant = await TenantBootstrapHelper.EnsureDefaultTenantAsync(_dbContext, cancellationToken);
        await TenantBootstrapHelper.EnsureSystemRolesForTenantAsync(_dbContext, tenant.Id, cancellationToken);

        var nextUserNumber = await _dbContext.Users.IgnoreQueryFilters().CountAsync(cancellationToken) + 1;
        var fieldRepRoleId = await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(role => role.TenantId == tenant.Id && role.Code == AppRoles.FieldRepresentative)
            .Select(role => role.Id)
            .FirstAsync(cancellationToken);

        var administratorRoleId = await _dbContext.Roles
            .IgnoreQueryFilters()
            .Where(role => role.TenantId == tenant.Id && role.Code == AppRoles.Administrator)
            .Select(role => role.Id)
            .FirstAsync(cancellationToken);

        var user = new User
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserCode = $"usr{nextUserNumber}",
            FullName = command.FullName.Trim(),
            Email = command.Email.Trim().ToLowerInvariant(),
            Phone = string.Empty,
            RoleId = fieldRepRoleId,
            WorkingHoursStart = new TimeOnly(9, 0),
            WorkingHoursEnd = new TimeOnly(18, 0),
            Status = UserStatus.Active,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var account = new AuthAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            Username = user.Email,
            PasswordHash = _credentialHasher.Hash(command.Password),
            AuthProvider = AuthProvider.Local,
            MustChangePassword = false,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        };

        var administratorRole = new UserRoleAssignment
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            RoleId = administratorRoleId,
            EffectiveFrom = DateTimeOffset.UtcNow,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var roles = new[] { AppRoles.Administrator };
        var actions = await _actionAuthorizationService.GetActionsForRolesAsync(roles, cancellationToken);
        var accessToken = _tokenService.CreateAccessToken(new AuthenticatedPrincipal(
            tenant.Id,
            user.Id,
            user.Email,
            user.FullName,
            roles,
            actions,
            account.TokenVersion));
        var refreshToken = _tokenService.CreateOpaqueToken();

        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            TenantId = tenant.Id,
            UserId = user.Id,
            RefreshTokenHash = _credentialHasher.Hash(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow
        };

        _dbContext.Users.Add(user);
        _dbContext.AuthAccounts.Add(account);
        _dbContext.UserRoleAssignments.Add(administratorRole);
        _dbContext.UserSessions.Add(session);

        return AppResult<AuthResponse>.Success(
            AuthDtoFactory.ToAuthResponse(user, tenant.Slug, roles, actions, accessToken, refreshToken, account.MustChangePassword));
    }
}
