namespace ItTms.Web.Models.Realtime;

/// <summary>
/// DTO truyền tải thông báo thời gian thực tới chuông Navbar.
/// </summary>
public sealed class NotificationDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Link { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedTime => CreatedAt.ToLocalTime().ToString("HH:mm dd/MM");
}
