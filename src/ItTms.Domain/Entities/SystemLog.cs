using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class SystemLog : EntityBase<int>
{
    public SystemLog() { }

    public int? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual User? User { get; set; }

    public static Result<SystemLog> Create(
        string action,
        string details,
        int? userId = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            return Result<SystemLog>.Failure(new Error("LOG_ACTION_REQUIRED", "Hành động nhật ký không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(details))
            return Result<SystemLog>.Failure(new Error("LOG_DETAILS_REQUIRED", "Chi tiết nhật ký không được để trống.", ErrorType.Validation));

        var log = new SystemLog
        {
            UserId = userId,
            Action = action.Trim(),
            Details = details.Trim(),
            IpAddress = ipAddress?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result<SystemLog>.Success(log);
    }
}
