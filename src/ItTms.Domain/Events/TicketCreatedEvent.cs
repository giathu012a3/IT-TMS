using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

public sealed record TicketCreatedEvent(
    int TicketId,
    string Title,
    int CreatorId,
    DateTime OccurredOn
) : IDomainEvent;
