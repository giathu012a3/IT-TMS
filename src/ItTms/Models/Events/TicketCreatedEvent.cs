

namespace ItTms.Models.Events;

public sealed record TicketCreatedEvent(
    int TicketId,
    string Title,
    int CreatorId,
    DateTime OccurredOn
) : IDomainEvent;
