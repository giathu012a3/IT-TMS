using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Domain.States;

/// <summary>
/// Trạng thái Chờ phản hồi (StatusId = 4): Kỹ thuật viên tạm dừng để chờ người dùng cung cấp thêm thông tin.
/// Cho phép tiếp tục xử lý (StartProgress), hoàn thành (Resolve), hoặc điều chuyển (Assign/Reassign).
/// </summary>
public sealed class WaitingState : TicketStateBase
{
    public WaitingState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Waiting;
    public override string Name => "Waiting";
    public override string DisplayLabel => "Chờ phản hồi";
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
}
