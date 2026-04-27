namespace Template.Modules.Common.Domain;

public abstract class BaseEntity : AuditableEntity
{
    public Guid Id { get; set; }
}
