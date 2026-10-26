namespace ItTms.Domain.Common;

/// <summary>
/// Giao diện cho các thực thể hoặc Aggregate Root có khả năng phát sinh Domain Events.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
