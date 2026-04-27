using Template.Modules.Common.Domain;

namespace Template.Modules.Modules.Users.Domain;

public enum UserStatus
{
    Active = 1,
    Offline = 2,
    Idle = 3,
    Late = 4,
    Inactive = 5
}

public sealed class User : BaseEntity, ITenantScopedEntity
{
    public Guid TenantId { get; set; }
    public string UserCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public Guid RoleId { get; set; }
    public TimeOnly WorkingHoursStart { get; set; }
    public TimeOnly WorkingHoursEnd { get; set; }
    public UserStatus Status { get; set; } = UserStatus.Active;
    public bool IsActive { get; set; } = true;
}
