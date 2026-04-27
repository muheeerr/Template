using Template.Modules.Common.Extensions;
using Template.Modules.Modules.Users.Domain;

namespace Template.Modules.Modules.Auth.Domain;

public sealed record AuthResponse(
    Guid TenantId,
    string TenantSlug,
    Guid UserId,
    string Email,
    string FullName,
    IReadOnlyCollection<string> Roles,
    string AvatarInitials,
    string Token,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    IReadOnlyCollection<string> Actions,
    bool MustChangePassword);

public sealed record CurrentUserResponse(
    Guid TenantId,
    string TenantSlug,
    Guid UserId,
    string Email,
    string FullName,
    string AvatarInitials,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Actions,
    bool MustChangePassword);

public static class AuthDtoFactory
{
    public static AuthResponse ToAuthResponse(
        User user,
        string tenantSlug,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> actions,
        AccessTokenDescriptor accessToken,
        string refreshToken,
        bool mustChangePassword)
    {
        return new AuthResponse(
            user.TenantId,
            tenantSlug,
            user.Id,
            user.Email,
            user.FullName,
            roles,
            user.FullName.ToAvatarInitials(),
            accessToken.Token,
            refreshToken,
            accessToken.ExpiresAt,
            actions,
            mustChangePassword);
    }

    public static CurrentUserResponse ToCurrentUserResponse(
        User user,
        string tenantSlug,
        IReadOnlyCollection<string> roles,
        IReadOnlyCollection<string> actions,
        bool mustChangePassword)
    {
        return new CurrentUserResponse(
            user.TenantId,
            tenantSlug,
            user.Id,
            user.Email,
            user.FullName,
            user.FullName.ToAvatarInitials(),
            roles,
            actions,
            mustChangePassword);
    }
}
