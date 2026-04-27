using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules.Modules.Users.Features;

public sealed class UserLookups : IUserLookups
{
    public void Map(IEndpointRouteBuilder app)
    {
        app.MapGet("/user-roles", async (TemplateDbContext dbContext, CancellationToken cancellationToken) =>
            {
                var roles = await dbContext.Roles
                    .AsNoTracking()
                    .Where(role => role.IsActive)
                    .OrderBy(role => role.Name)
                    .Select(role => new { id = role.Id, code = role.Code, name = role.Name })
                    .ToListAsync(cancellationToken);
                return Results.Ok(roles);
            })
            .RequireAuthorization();

        app.MapGet("/user-statuses", () => Results.Ok(new[]
            {
                new { value = "active", label = "Active" },
                new { value = "offline", label = "Offline" },
                new { value = "idle", label = "Idle" },
                new { value = "late", label = "Late" },
                new { value = "inactive", label = "Inactive" }
            }))
            .RequireAuthorization();
    }
}
