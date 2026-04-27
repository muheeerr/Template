using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Template.Modules.Modules.Auth.Domain;

public sealed class RoleActionAssignmentConfiguration : IEntityTypeConfiguration<RoleActionAssignment>
{
    public void Configure(EntityTypeBuilder<RoleActionAssignment> builder)
    {
        builder.ToTable("role_action_assignments");
        builder.HasIndex(x => new { x.RoleCode, x.ApiActionId }).IsUnique();
        builder.Property(x => x.RoleCode).HasMaxLength(100);
    }
}
