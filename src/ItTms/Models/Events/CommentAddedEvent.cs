

namespace ItTms.Models.Events;

public sealed record CommentAddedEvent(
    int CommentId,
    int TicketId,
    int AuthorUserId,
    string Content,
    bool IsInternal,
    DateTime OccurredOn
) : IDomainEvent;
