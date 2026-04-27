using System.Security.Claims;
using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Authorization;
using Microsoft.AspNetCore.Http;

namespace Template.Infrastructure.Observability;

public sealed class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated => Principal.Identity?.IsAuthenticated ?? false;

    public Guid? UserId =>
        Guid.TryParse(Principal.FindFirstValue(ClaimTypes.NameIdentifier), out var userId)
            ? userId
            : null;

    public string? Email => Principal.FindFirstValue(ClaimTypes.Email);

    public IReadOnlyCollection<string> Roles =>
        Principal.Claims
            .Where(claim => claim.Type == ClaimTypes.Role)
            .Select(claim => claim.Value)
            .ToArray();

    public IReadOnlyCollection<string> Actions =>
        Principal.Claims
            .Where(claim => claim.Type == AppClaimTypes.Action)
            .Select(claim => claim.Value)
            .ToArray();

    public ClaimsPrincipal Principal => _httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal(new ClaimsIdentity());

    public bool IsInRole(string role) => Principal.IsInRole(role);
}
