namespace Template.Modules.Modules.Auth.Domain;

public sealed record AccessTokenDescriptor(string Token, DateTimeOffset ExpiresAt);

public sealed record AuthenticatedPrincipal(
    Guid TenantId,
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Actions,
    int TokenVersion);

public sealed class BootstrapOptions
{
    public bool Enabled { get; set; } = true;
}

public interface ICredentialHasher
{
    string Hash(string value);

    bool Verify(string hash, string value);
}

public interface ITokenService
{
    AccessTokenDescriptor CreateAccessToken(AuthenticatedPrincipal principal);

    string CreateOpaqueToken();
}

public interface IActionAuthorizationService
{
    Task<IReadOnlyCollection<string>> GetActionsForRolesAsync(IReadOnlyCollection<string> roles, CancellationToken cancellationToken);
}

public interface IApiActionCatalogSynchronizer
{
    Task SyncAsync(CancellationToken cancellationToken);
}
