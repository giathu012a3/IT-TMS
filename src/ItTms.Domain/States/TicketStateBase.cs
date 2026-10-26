using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả các trạng thái cụ thể của Ticket.
/// Áp dụng mẫu Fail-Closed: Mọi hành vi mặc định đều bị từ chối trừ khi được ghi đè hợp lệ.
/// </summary>
public abstract class TicketStateBase : ITicketState
{
    protected Ticket Ticket { get; }

    protected TicketStateBase(Ticket ticket)
    {
        Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket), "Ngữ cảnh Ticket không được null.");
    }

    public abstract int StatusId { get; }
    public abstract string Name { get; }
    public abstract string DisplayLabel { get; }

    /// <summary>
    /// Mặc định cho phép bình luận, các trạng thái Resolved, Closed, Rejected sẽ ghi đè trả về false.
    /// </summary>
    public virtual bool CanComment => true;

    public virtual Result Assign(User staff, User? leader = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Assign)));

    public virtual Result Assign(int staffId, int leaderId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Assign)));

    public virtual Result Reassign(User staff, User? leader = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reassign)));

    public virtual Result Reassign(int staffId, int leaderId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reassign)));

    public virtual Result StartProgress(User? staff = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(StartProgress)));

    public virtual Result StartProgress(int staffId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(StartProgress)));

    public virtual Result HoldForInfo(string reason, User? staff = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(HoldForInfo)));

    public virtual Result HoldForInfo(string reason, int staffId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(HoldForInfo)));

    public virtual Result Resolve(User? staff = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Resolve)));

    public virtual Result Resolve(int staffId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Resolve)));

    public virtual Result Close(Feedback feedback, User? user = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Close)));

    public virtual Result Close(int userId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Close)));

    public virtual Result Reopen(string reason, User? user = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reopen)));

    public virtual Result Reopen(int userId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reopen)));

    public virtual Result Reject(string reason, User? leader = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reject)));

    public virtual Result Reject(string reason, int leaderId) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reject)));
}
