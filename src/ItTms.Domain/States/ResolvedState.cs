using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Domain.States;

/// <summary>
/// Trạng thái Đã giải quyết (StatusId = 5): Kỹ thuật viên đã xử lý xong và bàn giao cho User nghiệm thu.
/// Ràng buộc bất biến Bảng 7.2 TC-08: CanComment = false (Khóa bình luận).
/// Cho phép User nghiệm thu đóng ticket (Close - Bảng 7.2 TC-09) hoặc mở lại để xử lý tiếp (Reopen).
/// </summary>
public sealed class ResolvedState : TicketStateBase
{
    public ResolvedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Resolved;
    public override string Name => "Resolved";
    public override string DisplayLabel => "Đã giải quyết";

    // Ràng buộc bất biến Bảng 7.2 TC-08: Resolved khóa chức năng bình luận
    public override bool CanComment => false;

    public override Result Close(Feedback feedback, User? user = null)
    {
        var userId = user?.Id ?? Ticket.CreatorId;
        if (Ticket.CreatorId != userId)
            return Result.Failure(TicketErrors.UnauthorizedUser);

        if (feedback == null)
            return Result.Failure(TicketErrors.FeedbackRequired);

        Ticket.Feedback = feedback;
        Ticket.ClosedAt = DateTime.UtcNow;
        Ticket.TransitionTo(new ClosedState(Ticket), userId);

        Ticket.AddDomainEvent(new TicketClosedEvent(
            Ticket.Id,
            userId,
            feedback.Rating,
            feedback.Comment,
            DateTime.UtcNow));

        return Result.Success();
    }

    public override Result Close(int userId)
    {
        if (Ticket.CreatorId != userId)
            return Result.Failure(TicketErrors.UnauthorizedUser);

        Ticket.ClosedAt = DateTime.UtcNow;
        Ticket.TransitionTo(new ClosedState(Ticket), userId);

        // Phát sinh sự kiện đóng ticket với đánh giá mặc định nếu đóng bằng userId
        var rating = Ticket.Feedback?.Rating ?? 5;
        var comment = Ticket.Feedback?.Comment ?? "Xác nhận đóng hoàn tất";
        Ticket.AddDomainEvent(new TicketClosedEvent(
            Ticket.Id,
            userId,
            rating,
            comment,
            DateTime.UtcNow));

        return Result.Success();
    }

    public override Result Reopen(string reason, User? user = null)
    {
        var userId = user?.Id ?? Ticket.CreatorId;
        if (Ticket.CreatorId != userId)
            return Result.Failure(TicketErrors.UnauthorizedUser);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(TicketErrors.ReopenReasonRequired);

        return Reopen(userId);
    }

    public override Result Reopen(int userId)
    {
        if (Ticket.CreatorId != userId)
            return Result.Failure(TicketErrors.UnauthorizedUser);

        Ticket.ResolvedAt = null;
        Ticket.TransitionTo(new InProgressState(Ticket), userId);
        return Result.Success();
    }
}
