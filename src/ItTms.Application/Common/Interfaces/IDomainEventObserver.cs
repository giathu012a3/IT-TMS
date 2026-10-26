using ItTms.Domain.Common;

namespace ItTms.Application.Common.Interfaces;

/// <summary>
/// Định nghĩa Observer xử lý một sự kiện Domain Event cụ thể.
/// </summary>
public interface IDomainEventObserver<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
