using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

public sealed record TicketStatusChangedEvent(
    int TicketId,
    int OldStatusId,
    int NewStatusId,
    int ChangedByUserId,
    DateTime OccurredOn
) : IDomainEvent;
