using Microsoft.AspNetCore.Routing;

namespace Template.Modules.Common.Authorization;

public static class ApiActionPath
{
    public static string? FromEndpoint(Endpoint? endpoint)
    {
        return endpoint is RouteEndpoint routeEndpoint
            ? Normalize(routeEndpoint.RoutePattern.RawText)
            : null;
    }

    public static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "/";
        }

        var normalized = path.Trim();
        if (!normalized.StartsWith('/'))
        {
            normalized = $"/{normalized}";
        }

        while (normalized.Contains("//", StringComparison.Ordinal))
        {
            normalized = normalized.Replace("//", "/", StringComparison.Ordinal);
        }

        normalized = normalized.TrimEnd('/');
        return string.IsNullOrWhiteSpace(normalized)
            ? "/"
            : normalized.ToLowerInvariant();
    }

    public static bool IsApiPath(string path)
    {
        return path.StartsWith("/api/", StringComparison.OrdinalIgnoreCase);
    }
}
