namespace Template.Modules.Common.Contracts;

public sealed record ApiEnvelope<T>(
    bool Success,
    T? Data,
    string Message,
    IReadOnlyCollection<ApiError> Errors,
    object? Meta = null);

public sealed record ApiError(string? Field, string Code, string Message);

public sealed record PagedMeta(int Page, int PageSize, int TotalRecords, int TotalPages);
