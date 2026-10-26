namespace ItTms.Models.Realtime;

/// <summary>
/// DTO truyền tải bình luận thời gian thực qua SignalR WebSocket.
/// </summary>
public sealed class CommentDto
{
    public int Id { get; set; }
    public int TicketId { get; set; }
    public int AuthorUserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedTime => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}
