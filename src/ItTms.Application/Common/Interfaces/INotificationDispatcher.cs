using ItTms.Domain.Common;

namespace ItTms.Application.Common.Interfaces;

/// <summary>
/// Giao diện điều phối thông báo thời gian thực qua SignalR WebSocket.
/// </summary>
public interface INotificationDispatcher
{
    Task SendToUserAsync(int userId, string message, string link, CancellationToken cancellationToken = default);
    Task SendToRoleAsync(string roleName, string message, string link, CancellationToken cancellationToken = default);
    Task NotifyTicketGroupAsync(int ticketId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task DispatchDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
