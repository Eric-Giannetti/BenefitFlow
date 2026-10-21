namespace BenefitFlow.Domain.Common;

public sealed class Result
{
    private Result(bool isSuccess, DomainError? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public DomainError? Error { get; }

    public static Result Success() => new(true, null);

    public static Result Failure(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result(false, error);
    }

    public static Result<T> Success<T>(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(true, value, null);
    }

    public static Result<T> Failure<T>(DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new Result<T>(false, default, error);
    }
}

public sealed class Result<T>
{
    internal Result(bool isSuccess, T? value, DomainError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public bool IsFailure => !IsSuccess;

    public T? Value { get; }

    public DomainError? Error { get; }
}
