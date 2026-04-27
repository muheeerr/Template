using Template.Modules.Common.Extensions;
using Template.Modules.Common.Results;
using Template.Modules.Modules.Auth.Domain;
using Mediator;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Auth.Features;

public sealed class RefreshToken : IAuth
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapPost("/refresh", async (RefreshTokenCommand command, IMediator mediator, CancellationToken cancellationToken) =>
            (await mediator.Send(command, cancellationToken)).ToApiResult("Token refreshed successfully"))
            .AllowAnonymous();
    }
}

public sealed record RefreshTokenCommand(string RefreshToken) : IAppCommand<AuthResponse>;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, AppResult<AuthResponse>>
{
    private readonly TemplateDbContext _dbContext;
    private readonly ICredentialHasher _credentialHasher;
    private readonly ITokenService _tokenService;
    private readonly IActionAuthorizationService _actionAuthorizationService;

    public RefreshTokenHandler(
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

    public async ValueTask<AppResult<AuthResponse>> Handle(RefreshTokenCommand command, CancellationToken cancellationToken)
    {
        var sessions = await _dbContext.UserSessions
            .IgnoreQueryFilters()
            .Where(session => session.RevokedAt == null && session.ExpiresAt > DateTimeOffset.UtcNow && !session.IsDeleted)
            .ToListAsync(cancellationToken);

        var session = sessions.FirstOrDefault(activeSession => _credentialHasher.Verify(activeSession.RefreshTokenHash, command.RefreshToken));
        if (session is null)
        {
            return AppResult<AuthResponse>.Failure(Errors.Unauthorized("Refresh token is invalid or expired."));
        }

        session.RevokedAt = DateTimeOffset.UtcNow;

        var user = await _dbContext.Users
            .IgnoreQueryFilters()
            .FirstAsync(x => x.Id == session.UserId && x.TenantId == session.TenantId && !x.IsDeleted, cancellationToken);
        var account = await _dbContext.AuthAccounts
            .IgnoreQueryFilters()
            .FirstAsync(x => x.TenantId == session.TenantId && x.UserId == user.Id && !x.IsDeleted, cancellationToken);
        var roles = await _dbContext.UserRoleAssignments
            .IgnoreQueryFilters()
            .Where(x => x.TenantId == session.TenantId && x.UserId == user.Id && x.IsActive && !x.IsDeleted)
            .Join(
                _dbContext.Roles.IgnoreQueryFilters().Where(r => r.TenantId == session.TenantId && !r.IsDeleted),
                era => era.RoleId,
                role => role.Id,
                (_, role) => role.Code)
            .ToArrayAsync(cancellationToken);
        var actions = await _actionAuthorizationService.GetActionsForRolesAsync(roles, cancellationToken);

        var tenant = await _dbContext.Tenants
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstAsync(t => t.Id == session.TenantId && !t.IsDeleted, cancellationToken);

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
            TenantId = session.TenantId,
            UserId = user.Id,
            RefreshTokenHash = _credentialHasher.Hash(refreshToken),
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(14),
            CreatedAt = DateTimeOffset.UtcNow
        });

        return AppResult<AuthResponse>.Success(
            AuthDtoFactory.ToAuthResponse(user, tenant.Slug, roles, actions, accessToken, refreshToken, account.MustChangePassword));
    }
}
