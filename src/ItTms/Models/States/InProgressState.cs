



namespace ItTms.Models.States;

/// <summary>
/// Trạng thái Đang xử lý (StatusId = 3): Kỹ thuật viên đang thực hiện sửa chữa, khắc phục.
/// Cho phép tạm dừng (HoldForInfo), hoàn thành (Resolve), hoặc điều chuyển (Assign/Reassign).
/// Chặn đứng tuyệt đối hành vi Reject (Bảng 7.2 TC-06).
/// </summary>
public sealed class InProgressState : TicketStateBase
{
    public InProgressState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.InProgress;
    public override string Name => "InProgress";
    public override string DisplayLabel => "Đang xử lý";
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
        Ticket.TransitionTo(new AssignedState(Ticket), leaderId);
        Ticket.AddDomainEvent(new TicketAssignedEvent(Ticket.Id, staffId, leaderId, DateTime.UtcNow));
        return Result.Success();
    }

    public override Result HoldForInfo(string reason, User? staff = null)
    {
        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        return HoldForInfo(reason, staffId);
    }

    public override Result HoldForInfo(string reason, int staffId)
    {
        if (Ticket.AssignedToId.HasValue && Ticket.AssignedToId.Value != staffId)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        Ticket.TransitionTo(new WaitingState(Ticket), staffId);
        return Result.Success();
    }

    public override Result Resolve(User? staff = null)
    {
        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        return Resolve(staffId);
    }

    public override Result Resolve(int staffId)
    {
        if (Ticket.AssignedToId.HasValue && Ticket.AssignedToId.Value != staffId)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        Ticket.ResolvedAt = DateTime.UtcNow;
        Ticket.TransitionTo(new ResolvedState(Ticket), staffId);
        return Result.Success();
    }

    // Ràng buộc bất biến Bảng 7.2 TC-06: Leader không thể từ chối ticket đang InProgress
    public override Result Reject(string reason, User? leader = null) =>
        Result.Failure(TicketErrors.CannotRejectInProgress);

    public override Result Reject(string reason, int leaderId) =>
        Result.Failure(TicketErrors.CannotRejectInProgress);
}
