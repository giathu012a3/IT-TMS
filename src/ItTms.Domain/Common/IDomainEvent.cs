namespace ItTms.Domain.Common;

/// <summary>
/// Giao diện đánh dấu cho tất cả Domain Events.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}
