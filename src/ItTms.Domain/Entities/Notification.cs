using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class Notification : EntityBase<int>
{
    public Notification() { }

    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual User User { get; set; } = null!;

    public static Result<Notification> Create(int userId, string message, string link)
    {
        if (userId <= 0)
            return Result<Notification>.Failure(new Error("NOTIF_USER_INVALID", "Người nhận thông báo không hợp lệ.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(message))
            return Result<Notification>.Failure(new Error("NOTIF_MESSAGE_REQUIRED", "Nội dung thông báo không được để trống.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(link))
            return Result<Notification>.Failure(new Error("NOTIF_LINK_REQUIRED", "Đường dẫn liên kết không được để trống.", ErrorType.Validation));

        var notification = new Notification
        {
            UserId = userId,
            Message = message.Trim(),
            Link = link.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Notification>.Success(notification);
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
