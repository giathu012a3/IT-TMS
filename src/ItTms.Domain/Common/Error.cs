namespace ItTms.Domain.Common;

/// <summary>
/// Định nghĩa lỗi nghiệp vụ có cấu trúc (Railway-Oriented Programming).
/// </summary>
public readonly record struct Error(string Code, string Description, ErrorType Type = ErrorType.Failure)
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.Failure);
    public static readonly Error NullValue = new("ERROR_NULL_VALUE", "Giá trị truyền vào không được null.", ErrorType.Validation);

    public static Error Failure(string code, string description) => new(code, description, ErrorType.Failure);
    public static Error Validation(string code, string description) => new(code, description, ErrorType.Validation);
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Unauthorized(string code, string description) => new(code, description, ErrorType.Unauthorized);
    public static Error Forbidden(string code, string description) => new(code, description, ErrorType.Forbidden);

    public static implicit operator Result(Error error) => Result.Failure(error);
}
