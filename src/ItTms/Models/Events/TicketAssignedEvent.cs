

namespace ItTms.Models.Events;

public sealed record TicketAssignedEvent(
    int TicketId,
    int AssignedToId,
    int LeaderId,
    DateTime OccurredOn
) : IDomainEvent;
