using Template.Modules.Modules.Users.Domain;

namespace Template.Modules.Common.Extensions;

public static class DisplayValueExtensions
{
    public static string ToAvatarInitials(this string fullName)
    {
        return string.Concat(
                fullName
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Take(2)
                    .Select(part => char.ToUpperInvariant(part[0])))
            .PadRight(1, '?');
    }

    public static string ToApiValue(this UserStatus status)
    {
        return status.ToString().ToLowerInvariant();
    }
}
