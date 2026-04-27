using Template.Modules.Common.Authorization;
using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Tenants.Domain;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Persistence;

public static class TenantBootstrapHelper
{
    public const string DefaultSlug = "default";

    public static string NormalizeSlug(string slug) => slug.Trim().ToLowerInvariant();

    public static async Task<Tenant> EnsureDefaultTenantAsync(TemplateDbContext dbContext, CancellationToken cancellationToken)
    {
        var slug = NormalizeSlug(DefaultSlug);
        var existing = await dbContext.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Slug == slug, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }

        var utcNow = DateTimeOffset.UtcNow;
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Slug = slug,
            Name = "Default",
            IsActive = true,
            CreatedAt = utcNow,
            UpdatedAt = utcNow
        };

        dbContext.Tenants.Add(tenant);
        await dbContext.SaveChangesAsync(cancellationToken);
        return tenant;
    }

    public static async Task EnsureSystemRolesForTenantAsync(
        TemplateDbContext dbContext,
        Guid tenantId,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTimeOffset.UtcNow;

        var existingRoleCodes = await dbContext.Roles
            .IgnoreQueryFilters()
            .Where(role => role.TenantId == tenantId)
            .Select(role => role.Code)
            .ToListAsync(cancellationToken);

        (string Code, string Name)[] systemRoles =
        [
            (AppRoles.Administrator, "Administrator"),
            (AppRoles.FieldRepresentative, "Field Representative"),
            (AppRoles.SeniorFieldRepresentative, "Senior Field Representative"),
            (AppRoles.Planner, "Planner"),
            (AppRoles.Supervisor, "Supervisor"),
        ];

        foreach (var (code, name) in systemRoles.Where(r => !existingRoleCodes.Contains(r.Code)))
        {
            dbContext.Roles.Add(new AppRole
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Code = code,
                Name = name,
                IsSystem = true,
                IsActive = true,
                CreatedAt = utcNow,
                UpdatedAt = utcNow
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
