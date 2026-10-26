using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class TicketStatus : EntityBase<int>
{
    // Các hằng số định danh trạng thái chuẩn (Seed Data Ids)
    public const int New = 1;
    public const int Assigned = 2;
    public const int InProgress = 3;
    public const int Waiting = 4;
    public const int Resolved = 5;
    public const int Closed = 6;
    public const int Rejected = 7;

    public TicketStatus() { }

    public string Name { get; set; } = string.Empty;       // Mã code tiếng Anh: New, Assigned...
    public string Label { get; set; } = string.Empty;      // Nhãn tiếng Việt: Mới tạo, Đã phân công...
    public string ColorClass { get; set; } = string.Empty; // Class badge CSS Bootstrap 5
    public int SortOrder { get; set; }                     // Thứ tự hiển thị
    public string? Description { get; set; }

    // Navigation Properties
    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public static TicketStatus Create(int id, string name, string label, string colorClass, int sortOrder = 0, string? description = null)
    {
        return new TicketStatus
        {
            Id = id,
            Name = name.Trim(),
            Label = label.Trim(),
            ColorClass = colorClass.Trim(),
            SortOrder = sortOrder,
            Description = description?.Trim()
        };
    }
}
