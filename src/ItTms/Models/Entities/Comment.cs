

namespace ItTms.Models.Entities;

public class Comment : EntityBase<int>
{
    public Comment() { }

    public int TicketId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigations
    public virtual Ticket Ticket { get; set; } = null!;
    public virtual User User { get; set; } = null!;

    public static Result<Comment> Create(int ticketId, int userId, string content, bool isInternal = false)
    {
        if (ticketId <= 0)
            return Result<Comment>.Failure(new Error("COMMENT_TICKET_INVALID", "Mã ticket không hợp lệ.", ErrorType.Validation));

        if (userId <= 0)
            return Result<Comment>.Failure(new Error("COMMENT_USER_INVALID", "Mã người dùng không hợp lệ.", ErrorType.Validation));

        if (string.IsNullOrWhiteSpace(content))
            return Result<Comment>.Failure(new Error("COMMENT_CONTENT_REQUIRED", "Nội dung phản hồi không được để trống.", ErrorType.Validation));

        var comment = new Comment
        {
            TicketId = ticketId,
            UserId = userId,
            Content = content.Trim(),
            IsInternal = isInternal,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Comment>.Success(comment);
    }
}
