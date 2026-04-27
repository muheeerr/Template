using System.Security.Claims;

namespace Template.Modules.Common.Abstractions;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string? Email { get; }
    IReadOnlyCollection<string> Roles { get; }
    IReadOnlyCollection<string> Actions { get; }
    ClaimsPrincipal Principal { get; }
    bool IsInRole(string role);
}
