namespace ItTms.Models.Common;

/// <summary>
/// Kết quả thực thi nghiệp vụ không mang dữ liệu trả về (Railway-Oriented Monad).
/// </summary>
public class Result
{
    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
            throw new InvalidOperationException("Invalid Result state: Success cannot have error, Failure must have error.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    public static Result Failure(string code, string description, ErrorType type = ErrorType.Failure) => 
        new(false, new Error(code, description, type));
}

/// <summary>
/// Kết quả thực thi nghiệp vụ mang dữ liệu kiểu T.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error) 
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);
    public static new Result<T> Failure(string code, string description, ErrorType type = ErrorType.Failure) => 
        new(default, false, new Error(code, description, type));

    public static implicit operator Result<T>(T value) => Success(value);
    public static implicit operator Result<T>(Error error) => Failure(error);
}
