using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Common;
using ItTms.Domain.Events;
using Microsoft.Extensions.Logging;

namespace ItTms.Infrastructure.Realtime;

/// <summary>
/// Dịch vụ phân phối thông báo thời gian thực qua SignalR WebSocket.
/// </summary>
public class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    public SignalRNotificationDispatcher(ILogger<SignalRNotificationDispatcher> logger)
    {
        _logger = logger;
    }

    public Task SendToUserAsync(int userId, string message, string link, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SignalR notification to user {UserId}: {Message} ({Link})", userId, message, link);
        return Task.CompletedTask;
    }

    public Task SendToRoleAsync(string roleName, string message, string link, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SignalR notification to role {RoleName}: {Message} ({Link})", roleName, message, link);
        return Task.CompletedTask;
    }

    public Task NotifyTicketGroupAsync(int ticketId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("SignalR notification to ticket group {TicketId}: {EventName}", ticketId, eventName);
        return Task.CompletedTask;
    }

    public Task DispatchDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Dispatching domain event {EventType} to SignalR clients", domainEvent.GetType().Name);
        if (domainEvent is CommentAddedEvent comment)
        {
            return HandleCommentAddedAsync(comment, cancellationToken);
        }
        return Task.CompletedTask;
    }

    public Task HandleCommentAddedAsync(CommentAddedEvent comment, CancellationToken cancellationToken = default)
    {
        var group = comment.IsInternal ? $"ticket_{comment.TicketId}_internal" : $"ticket_{comment.TicketId}";
        _logger.LogInformation("SignalR push ReceiveComment to group {Group} for comment {CommentId}", group, comment.CommentId);
        return Task.CompletedTask;
    }
}
