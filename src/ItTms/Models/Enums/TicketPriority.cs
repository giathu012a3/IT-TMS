namespace ItTms.Models.Enums;

/// <summary>
/// Mức độ ưu tiên của yêu cầu hỗ trợ (quyết định hạn cam kết SLA DueAt).
/// </summary>
public enum TicketPriority
{
    Low = 1,        // SLA: 72 giờ
    Medium = 2,     // SLA: 24 giờ (Mặc định)
    High = 3,       // SLA: 4 giờ
    Critical = 4    // SLA: 1 giờ
}
