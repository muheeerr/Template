namespace Template.Modules.Common.Domain;

public interface ITenantScopedEntity
{
    Guid TenantId { get; set; }
}
