using Template.Modules.Common.Contracts;
using Template.Modules.Common.Results;
using HttpResults = Microsoft.AspNetCore.Http.Results;

namespace Template.Modules.Common.Extensions;

public static class ResultExtensions
{
    public static IResult ToApiResult(this AppResult result, string successMessage = "Operation completed successfully")
    {
        if (result.IsSuccess)
        {
            return HttpResults.Ok(new ApiEnvelope<object?>(true, null, successMessage, []));
        }

        var firstError = result.Errors[0];
        return HttpResults.Json(
            new ApiEnvelope<object?>(
                false,
                null,
                firstError.Message,
                result.Errors.Select(error => new ApiError(error.Field, error.Code, error.Message)).ToArray()),
            statusCode: firstError.StatusCode);
    }

    public static IResult ToApiResult<T>(this AppResult<T> result, string successMessage = "Operation completed successfully")
    {
        if (result.IsSuccess)
        {
            return HttpResults.Ok(new ApiEnvelope<T>(true, result.Value, successMessage, []));
        }

        var firstError = result.Errors[0];
        return HttpResults.Json(
            new ApiEnvelope<object?>(
                false,
                null,
                firstError.Message,
                result.Errors.Select(error => new ApiError(error.Field, error.Code, error.Message)).ToArray()),
            statusCode: firstError.StatusCode);
    }
}
