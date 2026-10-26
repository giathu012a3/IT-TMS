using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ItTms.Infrastructure.Events;

/// <summary>
/// Bộ điều phối Domain Events trong bộ nhớ theo 2 pha độc lập:
/// Pha 1 (In-transaction): Kích hoạt các Observer (NotificationObserver, SystemLogObserver) nạp dữ liệu vào DbContext.
/// Pha 2 (Post-commit): Phát sóng thông báo thời gian thực qua SignalR WebSocket.
/// </summary>
public sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationDispatcher? _realtimeDispatcher;
    private readonly ILogger<InMemoryDomainEventDispatcher> _logger;

    [ActivatorUtilitiesConstructor]
    public InMemoryDomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<InMemoryDomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public InMemoryDomainEventDispatcher(
        IServiceProvider serviceProvider,
        ILogger<InMemoryDomainEventDispatcher> logger,
        INotificationDispatcher? realtimeDispatcher)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _realtimeDispatcher = realtimeDispatcher;
    }

    public async Task DispatchInTransactionAsync(
        IEnumerable<IDomainEvent> domainEvents, 
        CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            var eventType = domainEvent.GetType();
            var handlerType = typeof(IDomainEventObserver<>).MakeGenericType(eventType);

            var handlers = _serviceProvider.GetServices(handlerType);

            foreach (var handler in handlers)
            {
                if (handler == null) continue;

                var method = handlerType.GetMethod("HandleAsync");
                if (method != null)
                {
                    try
                    {
                        var task = (Task)method.Invoke(handler, [domainEvent, cancellationToken])!;
                        await task;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Lỗi xảy ra trong quá trình xử lý in-transaction domain event {EventType}", eventType.Name);
                        throw; // Rollback transaction bảo toàn ACID
                    }
                }
            }
        }
    }

    public async Task DispatchPostCommitAsync(
        IEnumerable<IDomainEvent> domainEvents, 
        CancellationToken cancellationToken = default)
    {
        var dispatcher = _realtimeDispatcher ?? _serviceProvider.GetService<INotificationDispatcher>();
        if (dispatcher == null) return;

        foreach (var domainEvent in domainEvents)
        {
            try
            {
                await dispatcher.DispatchDomainEventAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Không thể phát sóng thời gian thực cho sự kiện {EventType} qua SignalR", domainEvent.GetType().Name);
            }
        }
    }
}
