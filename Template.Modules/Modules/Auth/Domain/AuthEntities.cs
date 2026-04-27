using Template.Modules.Common.Domain;

namespace Template.Modules.Modules.Auth.Domain;

public enum AuthProvider
{
    Local = 1,
    Google = 2,
    AzureAd = 3,
    Sso = 4
}

public enum RoleAssignmentScopeType
{
    Global = 1,
    Region = 2,
    StoreGroup = 3
}

public sealed class AuthAccount : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? PasswordHash { get; set; }
    public AuthProvider AuthProvider { get; set; } = AuthProvider.Local;
    public bool IsLocked { get; set; }
    /// <summary>When true (e.g. temporary password from onboarding), the client should force a password change.</summary>
    public bool MustChangePassword { get; set; }
    /// <summary>Stored TOTP secret for authenticator apps when MFA is enrolled; null until enrolled.</summary>
    public string? TotpSecret { get; set; }
    public int TokenVersion { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset? LastPasswordChangedAt { get; set; }
}

public sealed class UserRoleAssignment : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }
    public AppRole? Role { get; set; }
    public RoleAssignmentScopeType ScopeType { get; set; } = RoleAssignmentScopeType.Global;
    public Guid? ScopeId { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset EffectiveFrom { get; set; }
    public DateTimeOffset? EffectiveTo { get; set; }
}

public sealed class UserSession : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }
    public string RefreshTokenHash { get; set; } = string.Empty;
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
}

public sealed class ApiAction : BaseEntity
{
    public string Path { get; set; } = string.Empty;
    public string HttpMethods { get; set; } = string.Empty;
    public bool RequiresAuthorization { get; set; }
    public bool AllowAnonymous { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class AppRole : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;
}

public sealed class RoleActionAssignment : BaseEntity
{
    public string RoleCode { get; set; } = string.Empty;
    public Guid ApiActionId { get; set; }
    public bool IsActive { get; set; } = true;
}
