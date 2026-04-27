using Template.Modules;
using Template.Modules.Common.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Template.Infrastructure.Persistence;

public sealed class TemplateDbContextFactory : IDesignTimeDbContextFactory<TemplateDbContext>
{
    public TemplateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<TemplateDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=Template;Username=postgres;Password=postgres",
            npgsql =>
            {
                npgsql.EnableRetryOnFailure();
                npgsql.MigrationsAssembly(typeof(TemplateDbContextFactory).Assembly.GetName().Name);
            });

        return new TemplateDbContext(
            optionsBuilder.Options,
            UnauthenticatedCurrentUser.Instance,
            UnauthenticatedCurrentTenant.Instance);
    }
}
