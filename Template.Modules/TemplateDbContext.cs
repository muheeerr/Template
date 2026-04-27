using Template.Modules.Common.Abstractions;
using Template.Modules.Common.Domain;
using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Tenants.Domain;
using Template.Modules.Modules.Users.Domain;
using Template.Modules.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Template.Modules;

public sealed class TemplateDbContext(
    DbContextOptions<TemplateDbContext> options,
    ICurrentUser currentUser,
    ICurrentTenant currentTenant)
    : DbContext(options), IUnitOfWork
{
    private readonly ICurrentUser _currentUser = currentUser;
    private readonly ICurrentTenant _currentTenant = currentTenant;

    private Guid? TenantFilterValue => _currentTenant.TenantId;

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<AuthAccount> AuthAccounts => Set<AuthAccount>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<ApiAction> ApiActions => Set<ApiAction>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<RoleActionAssignment> RoleActionAssignments => Set<RoleActionAssignment>();
    public DbSet<User> Users => Set<User>();

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ApplyAuditingAndSoftDelete();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ApplyAuditingAndSoftDelete();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ApplyAuditingAndSoftDelete()
    {
        var utcNow = DateTimeOffset.UtcNow;
        var userId = _currentUser.UserId;

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
            }

            switch (entry.State)
            {
                case EntityState.Added:
                    if (entry.Entity.CreatedAt == default)
                    {
                        entry.Entity.CreatedAt = utcNow;
                    }

                    entry.Entity.UpdatedAt = utcNow;
                    if (userId.HasValue)
                    {
                        entry.Entity.CreatedBy = userId;
                        entry.Entity.UpdatedBy = userId;
                    }

                    break;
                case EntityState.Modified:
                    entry.Property(e => e.CreatedAt).IsModified = false;
                    entry.Property(e => e.CreatedBy).IsModified = false;
                    entry.Entity.UpdatedAt = utcNow;
                    if (userId.HasValue)
                    {
                        entry.Entity.UpdatedBy = userId;
                    }

                    break;
            }
        }

        NormalizeUtcDateTimeOffsets();
    }

    private void NormalizeUtcDateTimeOffsets()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified))
        {
            foreach (var property in entry.Properties
                .Where(p => p.Metadata.ClrType == typeof(DateTimeOffset) || p.Metadata.ClrType == typeof(DateTimeOffset?)))
            {
                if (property.CurrentValue is DateTimeOffset dto)
                {
                    property.CurrentValue = dto.ToUniversalTime();
                }
            }
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        var usePostgresExtensions = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";
        TemplateModelConventions.ConfigureModel(modelBuilder, usePostgresExtensions);

        modelBuilder.Entity<User>().HasQueryFilter(u =>
            !u.IsDeleted
            && u.IsActive
            && TenantFilterValue != null
            && u.TenantId == TenantFilterValue);

        modelBuilder.Entity<AuthAccount>().HasQueryFilter(a =>
            !a.IsDeleted
            && TenantFilterValue != null
            && a.TenantId == TenantFilterValue);

        modelBuilder.Entity<UserSession>().HasQueryFilter(s =>
            !s.IsDeleted
            && TenantFilterValue != null
            && s.TenantId == TenantFilterValue);

        modelBuilder.Entity<UserRoleAssignment>().HasQueryFilter(r =>
            !r.IsDeleted
            && r.IsActive
            && TenantFilterValue != null
            && r.TenantId == TenantFilterValue);

        modelBuilder.Entity<AppRole>().HasQueryFilter(r =>
            !r.IsDeleted
            && r.IsActive
            && TenantFilterValue != null
            && r.TenantId == TenantFilterValue);
    }
}
