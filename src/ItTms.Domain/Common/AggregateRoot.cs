namespace ItTms.Domain.Common;

/// <summary>
/// Lớp cơ sở cho Aggregate Root quản lý Domain Events.
/// </summary>
public abstract class AggregateRoot<TId> : EntityBase<TId>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
