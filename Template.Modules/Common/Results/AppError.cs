namespace Template.Modules.Common.Results;

public sealed record AppError(string Code, string Message, int StatusCode, string? Field = null);
