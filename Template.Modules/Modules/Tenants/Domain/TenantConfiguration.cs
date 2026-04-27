using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Template.Modules.Modules.Tenants.Domain;

public sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("tenants");
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.Property(x => x.Slug).HasMaxLength(100);
        builder.Property(x => x.Name).HasMaxLength(200);
    }
}
