using Template.Modules.Common.Domain;

namespace Template.Modules.Modules.Tenants.Domain;

public sealed class Tenant : BaseEntity
{
    public string Slug { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}
