namespace ItTms.Domain.Common;

/// <summary>
/// Phân loại lỗi nghiệp vụ theo chuẩn RFC 9457.
/// </summary>
public enum ErrorType
{
    Failure = 1,
    Validation = 2,
    NotFound = 3,
    Conflict = 4,
    Unauthorized = 5,
    Forbidden = 6
}
