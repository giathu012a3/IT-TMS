



namespace ItTms.Models.States;

/// <summary>
/// Trạng thái Đã phân công (StatusId = 2): Ticket đã được giao cho kỹ thuật viên.
/// Cho phép bắt đầu xử lý (StartProgress) hoặc điều chuyển kỹ thuật viên khác (Assign/Reassign).
/// </summary>
public sealed class AssignedState : TicketStateBase
{
    public AssignedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Assigned;
    public override string Name => "Assigned";
    public override string DisplayLabel => "Đã phân công";
    public override bool CanComment => true;

    public override Result Assign(User staff, User? leader = null) => Reassign(staff, leader);

    public override Result Assign(int staffId, int leaderId) => Reassign(staffId, leaderId);

    public override Result Reassign(User staff, User? leader = null)
    {
        if (staff == null || staff.Id <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        return Reassign(staff.Id, leader?.Id ?? 0);
    }

    public override Result Reassign(int staffId, int leaderId)
    {
        if (staffId <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        Ticket.AssignedToId = staffId;
        Ticket.UpdatedAt = DateTime.UtcNow;
        Ticket.RenewConcurrencyToken();

        Ticket.AddDomainEvent(new TicketAssignedEvent(Ticket.Id, staffId, leaderId, DateTime.UtcNow));
        return Result.Success();
    }

    public override Result StartProgress(User? staff = null)
    {
        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        return StartProgress(staffId);
    }

    public override Result StartProgress(int staffId)
    {
        if (Ticket.AssignedToId.HasValue && Ticket.AssignedToId.Value != staffId)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        Ticket.TransitionTo(new InProgressState(Ticket), staffId);
        return Result.Success();
    }
}
