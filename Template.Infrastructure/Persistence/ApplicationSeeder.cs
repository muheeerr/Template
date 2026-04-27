using Template.Modules;

using Template.Modules.Common.Authorization;

using Template.Modules.Persistence;

using Template.Modules.Modules.Auth.Domain;

using Template.Modules.Modules.Users.Domain;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.Configuration;



namespace Template.Infrastructure.Persistence;



public static class ApplicationSeeder

{

    public static async Task SeedAsync(

        TemplateDbContext dbContext,

        ICredentialHasher credentialHasher,

        IConfiguration configuration,

        CancellationToken cancellationToken = default)

    {

        await dbContext.Database.MigrateAsync(cancellationToken);



        var tenant = await TenantBootstrapHelper.EnsureDefaultTenantAsync(dbContext, cancellationToken);

        await TenantBootstrapHelper.EnsureSystemRolesForTenantAsync(dbContext, tenant.Id, cancellationToken);



        var utcNow = DateTimeOffset.UtcNow;



        var accountExists = await dbContext.AuthAccounts.IgnoreQueryFilters().AnyAsync(cancellationToken);

        if (!accountExists)

        {

            var email = (configuration["Superadmin:Email"] ?? "admin@template.local").Trim().ToLowerInvariant();

            var password = configuration["Superadmin:Password"] ?? "Admin@123456";



            var fieldRepRoleId = await dbContext.Roles

                .IgnoreQueryFilters()

                .Where(role => role.TenantId == tenant.Id && role.Code == AppRoles.FieldRepresentative)

                .Select(role => role.Id)

                .FirstAsync(cancellationToken);



            var administratorRoleId = await dbContext.Roles

                .IgnoreQueryFilters()

                .Where(role => role.TenantId == tenant.Id && role.Code == AppRoles.Administrator)

                .Select(role => role.Id)

                .FirstAsync(cancellationToken);



            var user = new User

            {

                Id = Guid.NewGuid(),

                TenantId = tenant.Id,

                UserCode = "superadmin",

                FullName = "Super Admin",

                Email = email,

                Phone = string.Empty,

                RoleId = fieldRepRoleId,

                WorkingHoursStart = new TimeOnly(9, 0),

                WorkingHoursEnd = new TimeOnly(18, 0),

                Status = UserStatus.Active,

                IsActive = true,

                CreatedAt = utcNow,

                UpdatedAt = utcNow

            };



            var account = new AuthAccount

            {

                Id = Guid.NewGuid(),

                TenantId = tenant.Id,

                UserId = user.Id,

                Username = user.Email,

                PasswordHash = credentialHasher.Hash(password),

                AuthProvider = AuthProvider.Local,

                MustChangePassword = false,

                CreatedAt = utcNow,

                UpdatedAt = utcNow

            };



            var roleAssignment = new UserRoleAssignment

            {

                Id = Guid.NewGuid(),

                TenantId = tenant.Id,

                UserId = user.Id,

                RoleId = administratorRoleId,

                EffectiveFrom = utcNow,

                CreatedAt = utcNow

            };



            dbContext.Users.Add(user);

            dbContext.AuthAccounts.Add(account);

            dbContext.UserRoleAssignments.Add(roleAssignment);



            await dbContext.SaveChangesAsync(cancellationToken);

        }

    }

}

