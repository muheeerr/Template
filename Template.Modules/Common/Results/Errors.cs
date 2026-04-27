using System.Net;

namespace Template.Modules.Common.Results;

public static class Errors
{
    public static AppError Validation(string field, string message) =>
        new("validation", message, (int)HttpStatusCode.BadRequest, field);

    public static AppError Conflict(string message, string? field = null) =>
        new("conflict", message, (int)HttpStatusCode.Conflict, field);

    public static AppError NotFound(string message) =>
        new("not_found", message, (int)HttpStatusCode.NotFound);

    public static AppError Unauthorized(string message = "Unauthorized") =>
        new("unauthorized", message, (int)HttpStatusCode.Unauthorized);

    public static AppError Forbidden(string message = "Forbidden") =>
        new("forbidden", message, (int)HttpStatusCode.Forbidden);

    public static AppError BusinessRule(string message, string? field = null) =>
        new("business_rule", message, (int)HttpStatusCode.UnprocessableEntity, field);
}
