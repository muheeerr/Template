namespace Template.Modules.Common.Results;

public class AppResult
{
    public bool IsSuccess { get; }
    public IReadOnlyList<AppError> Errors { get; }

    protected AppResult(bool isSuccess, IReadOnlyList<AppError> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static AppResult Success() => new(true, []);

    public static AppResult Failure(params AppError[] errors) => new(false, errors);

    public static AppResult Failure(IEnumerable<AppError> errors) => new(false, errors.ToArray());
}

public sealed class AppResult<T> : AppResult
{
    public T? Value { get; }

    private AppResult(bool isSuccess, T? value, IReadOnlyList<AppError> errors)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static AppResult<T> Success(T value) => new(true, value, []);

    public static new AppResult<T> Failure(params AppError[] errors) => new(false, default, errors);

    public static new AppResult<T> Failure(IEnumerable<AppError> errors) => new(false, default, errors.ToArray());
}
