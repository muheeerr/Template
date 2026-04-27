using Template.Modules.Modules.Tenants.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Template.Modules.Modules.Auth.Domain;

public sealed class AuthAccountConfiguration : IEntityTypeConfiguration<AuthAccount>
{
    public void Configure(EntityTypeBuilder<AuthAccount> builder)
    {
        builder.ToTable("auth_accounts");
        builder.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Username }).IsUnique();
        builder.Property(x => x.Username).HasMaxLength(150);
        builder.Property(x => x.TotpSecret).HasMaxLength(256);
    }
}
