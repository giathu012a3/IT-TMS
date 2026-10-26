using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

public sealed record TicketClosedEvent(
    int TicketId,
    int ClosedByUserId,
    int Rating,
    string? Comment,
    DateTime OccurredOn
) : IDomainEvent;
