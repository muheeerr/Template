using System.Security.Claims;

namespace Template.Modules.Common.Abstractions;

/// <summary>
/// Used when no HTTP context exists (design-time migrations, tests, background jobs).
/// </summary>
public sealed class UnauthenticatedCurrentUser : ICurrentUser
{
    public static readonly UnauthenticatedCurrentUser Instance = new();

    private UnauthenticatedCurrentUser()
    {
    }

    public bool IsAuthenticated => false;

    public Guid? UserId => null;

    public string? Email => null;

    public IReadOnlyCollection<string> Roles => Array.Empty<string>();

    public IReadOnlyCollection<string> Actions => Array.Empty<string>();

    public ClaimsPrincipal Principal => new(new ClaimsIdentity());

    public bool IsInRole(string role) => false;
}
