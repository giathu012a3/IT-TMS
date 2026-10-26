using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

/// <summary>
/// Trạng thái Đã đóng (StatusId = 6): Vòng đời ticket đã hoàn tất trọn vẹn (Terminal State).
/// CanComment = false. Mọi hành vi thay đổi trạng thái đều bị từ chối vĩnh viễn.
/// </summary>
public sealed class ClosedState : TicketStateBase
{
    public ClosedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Closed;
    public override string Name => "Closed";
    public override string DisplayLabel => "Đã đóng";
    public override bool CanComment => false;

    public override Result Assign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Assign(int staffId, int leaderId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reassign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reassign(int staffId, int leaderId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result StartProgress(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result StartProgress(int staffId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result HoldForInfo(string reason, User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result HoldForInfo(string reason, int staffId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Resolve(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Resolve(int staffId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Close(Feedback feedback, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Close(int userId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reopen(string reason, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reopen(int userId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reject(string reason, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reject(string reason, int leaderId) => Result.Failure(TicketErrors.TicketAlreadyClosed);
}
