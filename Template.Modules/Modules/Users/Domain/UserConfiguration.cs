using Template.Modules.Modules.Auth.Domain;
using Template.Modules.Modules.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Template.Modules.Modules.Users.Domain;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");
        builder.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.UserCode }).IsUnique();
        builder.HasIndex(x => x.RoleId);
        builder.Property(x => x.FullName).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(255);
        builder.Property(x => x.Phone).HasMaxLength(20);
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppRole>().WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Phone).IsUnique().HasFilter("phone <> ''");
    }
}
