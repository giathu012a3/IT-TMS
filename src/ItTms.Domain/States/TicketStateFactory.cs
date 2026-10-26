using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

/// <summary>
/// Bộ tạo và khôi phục thực thể State tương ứng với StatusId của Ticket.
/// Hỗ trợ EF Core khôi phục đúng hành vi trạng thái khi materialize entity từ CSDL.
/// </summary>
public static class TicketStateFactory
{
    public static ITicketState CreateState(Ticket ticket, int statusId)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        return statusId switch
        {
            TicketStatus.New => new NewState(ticket),
            TicketStatus.Assigned => new AssignedState(ticket),
            TicketStatus.InProgress => new InProgressState(ticket),
            TicketStatus.Waiting => new WaitingState(ticket),
            TicketStatus.Resolved => new ResolvedState(ticket),
            TicketStatus.Closed => new ClosedState(ticket),
            TicketStatus.Rejected => new RejectedState(ticket),
            _ => new NewState(ticket)
        };
    }
}
