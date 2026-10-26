


namespace ItTms.Models.States;

/// <summary>
/// Trạng thái Đã từ chối (StatusId = 7): Ticket bị Leader từ chối tiếp nhận (Terminal State).
/// CanComment = false. Mọi hành vi thay đổi trạng thái đều bị từ chối vĩnh viễn.
/// </summary>
public sealed class RejectedState : TicketStateBase
{
    public RejectedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Rejected;
    public override string Name => "Rejected";
    public override string DisplayLabel => "Đã từ chối";
    public override bool CanComment => false;

    public override Result Assign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Assign(int staffId, int leaderId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reassign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reassign(int staffId, int leaderId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result StartProgress(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result StartProgress(int staffId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result HoldForInfo(string reason, User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result HoldForInfo(string reason, int staffId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Resolve(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Resolve(int staffId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Close(Feedback feedback, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Close(int userId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reopen(string reason, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reopen(int userId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reject(string reason, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reject(string reason, int leaderId) => Result.Failure(TicketErrors.TicketAlreadyRejected);
}
