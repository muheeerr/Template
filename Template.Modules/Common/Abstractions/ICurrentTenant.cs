namespace Template.Modules.Common.Abstractions;

public interface ICurrentTenant
{
    Guid? TenantId { get; }
}
