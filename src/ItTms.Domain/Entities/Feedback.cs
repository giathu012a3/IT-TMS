using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class Feedback : EntityBase<int>
{
    public Feedback() { }

    public int TicketId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation
    public virtual Ticket Ticket { get; set; } = null!;

    public static Result<Feedback> Create(int ticketId, int rating, string? comment = null)
    {
        if (ticketId <= 0)
            return Result<Feedback>.Failure(new Error("FEEDBACK_TICKET_INVALID", "Mã ticket không hợp lệ.", ErrorType.Validation));

        if (rating < 1 || rating > 5)
            return Result<Feedback>.Failure(new Error("FEEDBACK_RATING_RANGE", "Điểm đánh giá phải nằm trong khoảng từ 1 đến 5 sao.", ErrorType.Validation));

        var feedback = new Feedback
        {
            TicketId = ticketId,
            Rating = rating,
            Comment = comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result<Feedback>.Success(feedback);
    }
}
