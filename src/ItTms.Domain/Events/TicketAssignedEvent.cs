using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

public sealed record TicketAssignedEvent(
    int TicketId,
    int AssignedToId,
    int LeaderId,
    DateTime OccurredOn
) : IDomainEvent;
