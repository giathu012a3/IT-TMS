using ItTms.Domain.Entities;

namespace ItTms.Domain.Extensions;

public static class TicketSlaExtensions
{
    public static bool IsOverdue(this Ticket ticket, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        // Ticket quá hạn nếu đã qua DueAt mà chưa Resolved, Closed hoặc Rejected
        return ticket.DueAt < now && 
               ticket.StatusId != TicketStatus.Resolved && 
               ticket.StatusId != TicketStatus.Closed &&
               ticket.StatusId != TicketStatus.Rejected;
    }

    public static TimeSpan RemainingTime(this Ticket ticket, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        return ticket.DueAt - now;
    }
}
