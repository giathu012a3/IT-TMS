



namespace ItTms.Models.States;

/// <summary>
/// Trạng thái Mới tạo (StatusId = 1): Ticket vừa được User gửi lên, nằm trong hàng đợi phân công của Leader.
/// Cho phép bình luận (CanComment = true). Cho phép phân công (Assign) hoặc từ chối tiếp nhận (Reject).
/// </summary>
public sealed class NewState : TicketStateBase
{
    public NewState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.New;
    public override string Name => "New";
    public override string DisplayLabel => "Mới tạo";
    public override bool CanComment => true;

    public override Result Assign(User staff, User? leader = null)
    {
        if (staff == null || staff.Id <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        return Assign(staff.Id, leader?.Id ?? 0);
    }

    public override Result Assign(int staffId, int leaderId)
    {
        if (staffId <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        Ticket.AssignedToId = staffId;
        Ticket.TransitionTo(new AssignedState(Ticket), leaderId);

        Ticket.AddDomainEvent(new TicketAssignedEvent(Ticket.Id, staffId, leaderId, DateTime.UtcNow));
        return Result.Success();
    }

    public override Result Reject(string reason, User? leader = null)
    {
        return Reject(reason, leader?.Id ?? 0);
    }

    public override Result Reject(string reason, int leaderId)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(TicketErrors.RejectionReasonRequired);

        Ticket.RejectionReason = reason.Trim();
        Ticket.TransitionTo(new RejectedState(Ticket), leaderId);
        return Result.Success();
    }
}
