using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using ItTms.Domain.Enums;

namespace ItTms.Web.Models.ViewModels;

public class CreateTicketViewModel
{
    [Required(ErrorMessage = "Tiêu đề yêu cầu không được để trống.")]
    [StringLength(150, ErrorMessage = "Tiêu đề không được vượt quá 150 ký tự.")]
    [Display(Name = "Tiêu đề yêu cầu")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mô tả chi tiết không được để trống.")]
    [Display(Name = "Mô tả chi tiết sự cố")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng chọn mức độ ưu tiên.")]
    [Display(Name = "Mức độ ưu tiên (SLA)")]
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    [Required(ErrorMessage = "Vui lòng chọn danh mục sự cố.")]
    [Display(Name = "Danh mục sự cố")]
    public TicketCategory Category { get; set; } = TicketCategory.Hardware;

    [Display(Name = "Tệp tin đính kèm (Tối đa 16MB)")]
    public IFormFile? Attachment { get; set; }
}

public class TicketListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public TicketCategory Category { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColorClass { get; set; } = "bg-secondary";
    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public bool IsOverdue => (StatusId != 5 && StatusId != 6 && StatusId != 7) && DateTime.UtcNow > DueAt;
    public int? FeedbackRating { get; set; }
}

public class TicketDetailsViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public TicketCategory Category { get; set; }
    public int StatusId { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public string StatusLabel { get; set; } = string.Empty;
    public string StatusColorClass { get; set; } = "bg-secondary";
    public bool CanComment { get; set; }

    public int CreatorId { get; set; }
    public string CreatorName { get; set; } = string.Empty;
    public string? CreatorEmail { get; set; }
    public string? CreatorDepartment { get; set; }

    public int? AssignedToId { get; set; }
    public string? AssignedToName { get; set; }
    public string? AssignedToEmail { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsOverdue => (StatusId != 5 && StatusId != 6 && StatusId != 7) && DateTime.UtcNow > DueAt;

    public List<CommentItemViewModel> Comments { get; set; } = [];
    public List<AttachmentItemViewModel> Attachments { get; set; } = [];
    public FeedbackItemViewModel? Feedback { get; set; }
}

public class CommentItemViewModel
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorRole { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
    public string FormattedTime => CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");
}

public class AttachmentItemViewModel
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FileSizeFormatted => FileSize < 1024 * 1024 
        ? $"{FileSize / 1024.0:F1} KB" 
        : $"{FileSize / (1024.0 * 1024.0):F2} MB";
    public DateTime CreatedAt { get; set; }
}

public class FeedbackItemViewModel
{
    public int Id { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CloseAndRateViewModel
{
    [Required]
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn số sao đánh giá (1-5 sao).")]
    [Range(1, 5, ErrorMessage = "Điểm đánh giá phải từ 1 đến 5 sao.")]
    public int Rating { get; set; } = 5;

    [Display(Name = "Ý kiến góp ý / Nhận xét thêm")]
    public string? Comment { get; set; }
}

public class AssignTicketViewModel
{
    [Required]
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn kỹ thuật viên tiếp nhận.")]
    public int StaffId { get; set; }
}

public class RejectTicketViewModel
{
    [Required]
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập lý do từ chối yêu cầu.")]
    [MinLength(5, ErrorMessage = "Lý do từ chối phải có ít nhất 5 ký tự.")]
    public string Reason { get; set; } = string.Empty;
}

public class ReassignTicketViewModel
{
    [Required]
    public int TicketId { get; set; }

    [Required(ErrorMessage = "Vui lòng chọn kỹ thuật viên mới.")]
    public int NewStaffId { get; set; }
}
