namespace ItTms.Web.Models.Realtime;

/// <summary>
/// DTO truyền tải thay đổi trạng thái ticket theo thời gian thực tới giao diện.
/// </summary>
public sealed class TicketStatusChangedDto
{
    public int TicketId { get; set; }
    public int OldStatusId { get; set; }
    public int NewStatusId { get; set; }
    public string NewStatusName { get; set; } = string.Empty;
    public string NewStatusLabel { get; set; } = string.Empty;
    public string ColorClass { get; set; } = string.Empty;
    public bool CanComment { get; set; }
    public DateTime UpdatedAt { get; set; }
}
