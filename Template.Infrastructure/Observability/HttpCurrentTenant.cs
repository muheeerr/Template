using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Authorization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Template.Infrastructure.Observability;

public sealed class HttpCurrentTenant : ICurrentTenant
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public HttpCurrentTenant(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? TenantId
    {
        get
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            if (principal is null)
            {
                return null;
            }

            return Guid.TryParse(principal.FindFirst(AppClaimTypes.TenantId)?.Value, out var id)
                ? id
                : null;
        }
    }
}
