namespace Template.Modules.Common.Abstractions;

/// <summary>
/// Used when no HTTP context exists (design-time migrations, tests, background jobs).
/// </summary>
public sealed class UnauthenticatedCurrentTenant : ICurrentTenant
{
    public static readonly UnauthenticatedCurrentTenant Instance = new();

    private UnauthenticatedCurrentTenant()
    {
    }

    public Guid? TenantId => null;
}
