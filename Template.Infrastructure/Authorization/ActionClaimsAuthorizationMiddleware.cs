using System.Security.Claims;
using Template.Modules.Common.Authorization;
using Template.Modules.Common.Contracts;
using Template.Modules.Common.Results;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace Template.Infrastructure.Authorization;

public sealed class ActionClaimsAuthorizationMiddleware
{
    private readonly RequestDelegate _next;

    public ActionClaimsAuthorizationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint is null)
        {
            await _next(context);
            return;
        }

        if (endpoint.Metadata.GetMetadata<IAllowAnonymous>() is not null)
        {
            await _next(context);
            return;
        }

        if (endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Count == 0)
        {
            await _next(context);
            return;
        }

        var actionPath = ApiActionPath.FromEndpoint(endpoint);
        if (actionPath is null || !ApiActionPath.IsApiPath(actionPath))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
        {
            await WriteUnauthorizedAsync(context, Errors.Unauthorized());
            return;
        }

        if (context.User.HasClaim(ClaimTypes.Role, AppRoles.Administrator))
        {
            await _next(context);
            return;
        }

        var allowedActions = context.User.Claims
            .Where(claim => claim.Type == AppClaimTypes.Action)
            .Select(claim => ApiActionPath.Normalize(claim.Value))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!allowedActions.Contains(actionPath))
        {
            await WriteUnauthorizedAsync(
                context,
                Errors.Unauthorized($"You do not have access to '{actionPath}'."));
            return;
        }

        await _next(context);
    }

    private static async Task WriteUnauthorizedAsync(HttpContext context, AppError error)
    {
        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.StatusCode = error.StatusCode;
        await context.Response.WriteAsJsonAsync(
            new ApiEnvelope<object?>(
                false,
                null,
                error.Message,
                [new ApiError(error.Field, error.Code, error.Message)]),
            cancellationToken: context.RequestAborted);
    }
}
