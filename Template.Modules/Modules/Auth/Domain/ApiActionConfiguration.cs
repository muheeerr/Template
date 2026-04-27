using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Template.Modules.Modules.Auth.Domain;

public sealed class ApiActionConfiguration : IEntityTypeConfiguration<ApiAction>
{
    public void Configure(EntityTypeBuilder<ApiAction> builder)
    {
        builder.ToTable("api_actions");
        builder.HasIndex(x => x.Path).IsUnique();
        builder.Property(x => x.Path).HasMaxLength(300);
        builder.Property(x => x.HttpMethods).HasMaxLength(100);
    }
}
