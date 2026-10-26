using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using ItTms.Web.Hubs;
using ItTms.Web.Models.Realtime;

namespace ItTms.Web.Services;

/// <summary>
/// Bộ phân phối thông báo và sự kiện thời gian thực qua SignalR WebSocket.
/// Hiện thực hóa INotificationDispatcher, triệt tiêu cơ chế polling 3s.
/// </summary>
public sealed class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<TicketHub> _hubContext;
    private readonly IItTmsDbContext _dbContext;
    private readonly ILogger<SignalRNotificationDispatcher> _logger;

    public SignalRNotificationDispatcher(
        IHubContext<TicketHub> hubContext,
        IItTmsDbContext dbContext,
        ILogger<SignalRNotificationDispatcher> logger)
    {
        _hubContext = hubContext;
        _dbContext = dbContext;
        _logger = logger;
    }

    /// <summary>
    /// Gửi thông báo tới chuông thông báo cá nhân của người dùng.
    /// </summary>
    public async Task SendToUserAsync(int userId, string message, string link, CancellationToken cancellationToken = default)
    {
        var payload = new NotificationDto
        {
            Id = 0,
            UserId = userId,
            Message = message,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        // Đẩy sự kiện ReceiveNotification tới nhóm user_{userId}
        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("ReceiveNotification", payload, cancellationToken);

        // Tính toán số lượng thông báo chưa đọc thực tế và đẩy sự kiện cập nhật badge
        var unreadCount = await _dbContext.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        await _hubContext.Clients.Group($"user_{userId}")
            .SendAsync("UpdateUnreadNotificationCount", unreadCount, cancellationToken);

        _logger.LogInformation("SignalR: Đã đẩy thông báo tới user_{UserId}: {Message}", userId, message);
    }

    /// <summary>
    /// Gửi thông báo diện rộng tới toàn bộ người dùng thuộc một vai trò nhất định (VD: Leader).
    /// </summary>
    public async Task SendToRoleAsync(string roleName, string message, string link, CancellationToken cancellationToken = default)
    {
        var payload = new NotificationDto
        {
            Id = 0,
            UserId = 0,
            Message = message,
            Link = link,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _hubContext.Clients.Group($"role_{roleName}")
            .SendAsync("ReceiveNotification", payload, cancellationToken);

        _logger.LogInformation("SignalR: Đã broadcast thông báo tới role_{Role}: {Message}", roleName, message);
    }

    /// <summary>
    /// Gửi sự kiện tùy ý tới phòng của 1 Ticket.
    /// </summary>
    public async Task NotifyTicketGroupAsync(int ticketId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"ticket_{ticketId}")
            .SendAsync(eventName, payload, cancellationToken);
        await _hubContext.Clients.Group($"Ticket_{ticketId}")
            .SendAsync(eventName, payload, cancellationToken);
    }

    /// <summary>
    /// Bắt và phân phối các Domain Events sau khi CSDL đã Commit thành công (Post-commit Phase).
    /// </summary>
    public async Task DispatchDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        switch (domainEvent)
        {
            case CommentAddedEvent comment:
                await HandleCommentAddedAsync(comment, cancellationToken);
                break;

            case TicketStatusChangedEvent statusChanged:
                await HandleStatusChangedAsync(statusChanged, cancellationToken);
                break;

            case TicketCreatedEvent created:
                await SendToRoleAsync(RoleNames.Leader, 
                    $"[Yêu cầu mới #{created.TicketId}] {created.Title}", 
                    "/Leader/Ticket/NewQueue", cancellationToken);
                break;

            case TicketAssignedEvent assigned:
                await HandleTicketAssignedAsync(assigned, cancellationToken);
                break;

            case TicketClosedEvent closed:
                await HandleTicketClosedAsync(closed, cancellationToken);
                break;

            case UserDeactivatedEvent deactivated:
                await _hubContext.Clients.Group($"user_{deactivated.UserId}")
                    .SendAsync("ForceLogout", "Tài khoản của bạn đã bị vô hiệu hóa bởi Quản trị viên.", cancellationToken);
                break;
        }
    }

    public async Task HandleCommentAddedAsync(CommentAddedEvent comment, CancellationToken cancellationToken = default)
    {
        var author = await _dbContext.Users
            .AsNoTracking()
            .Select(u => new { u.Id, u.FullName, u.Role })
            .FirstOrDefaultAsync(u => u.Id == comment.AuthorUserId, cancellationToken);

        var dto = new CommentDto
        {
            Id = comment.CommentId,
            TicketId = comment.TicketId,
            AuthorUserId = comment.AuthorUserId,
            AuthorName = author?.FullName ?? "Người dùng",
            AuthorRole = author?.Role.ToString() ?? "User",
            Content = comment.Content,
            IsInternal = comment.IsInternal,
            CreatedAt = comment.OccurredOn
        };

        if (comment.IsInternal)
        {
            await _hubContext.Clients.Group($"ticket_{comment.TicketId}_internal")
                .SendAsync("ReceiveComment", dto, cancellationToken);
            await _hubContext.Clients.Group($"Ticket_{comment.TicketId}_internal")
                .SendAsync("ReceiveComment", dto, cancellationToken);
        }
        else
        {
            await _hubContext.Clients.Group($"ticket_{comment.TicketId}")
                .SendAsync("ReceiveComment", dto, cancellationToken);
            await _hubContext.Clients.Group($"Ticket_{comment.TicketId}")
                .SendAsync("ReceiveComment", dto, cancellationToken);
        }

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.Title, t.CreatorId, t.AssignedToId })
            .FirstOrDefaultAsync(t => t.Id == comment.TicketId, cancellationToken);

        if (ticket == null) return;

        var authorName = author?.FullName ?? "Người dùng";
        var noteType = comment.IsInternal ? "ghi chú nội bộ" : "bình luận";

        // 1. Phân phối chuông thông báo cho User (Người tạo ticket): TUYỆT ĐỐI KHÔNG gửi nếu IsInternal == true hoặc User chính là tác giả
        if (!comment.IsInternal && comment.AuthorUserId != ticket.CreatorId)
        {
            await SendToUserAsync(
                ticket.CreatorId,
                $"[{authorName}] đã gửi phản hồi trên ticket #{ticket.Id}: {ticket.Title}",
                $"/User/Ticket/Details/{ticket.Id}",
                cancellationToken);
        }

        // 2. Phân phối chuông thông báo cho Kỹ thuật viên (Staff phụ trách nếu có và khác tác giả)
        if (ticket.AssignedToId.HasValue && ticket.AssignedToId.Value != comment.AuthorUserId)
        {
            await SendToUserAsync(
                ticket.AssignedToId.Value,
                $"[{authorName}] đã thêm {noteType} trên ticket #{ticket.Id}: {ticket.Title}",
                $"/Staff/Ticket/Details/{ticket.Id}",
                cancellationToken);
        }

        // 3. Phân phối chuông thông báo cho Trưởng nhóm IT (Leader)
        var leaderIds = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Leader && u.Status == UserStatus.Active && u.Id != comment.AuthorUserId && u.Id != ticket.AssignedToId)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        foreach (var leaderId in leaderIds)
        {
            await SendToUserAsync(
                leaderId,
                $"[{authorName}] đã thêm {noteType} trên ticket #{ticket.Id}: {ticket.Title}",
                $"/Leader/Ticket/Details/{ticket.Id}",
                cancellationToken);
        }
    }

    private async Task HandleStatusChangedAsync(TicketStatusChangedEvent ev, CancellationToken cancellationToken)
    {
        var status = await _dbContext.TicketStatuses
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == ev.NewStatusId, cancellationToken);

        var payload = new TicketStatusChangedDto
        {
            TicketId = ev.TicketId,
            OldStatusId = ev.OldStatusId,
            NewStatusId = ev.NewStatusId,
            NewStatusName = status?.Name ?? "Unknown",
            NewStatusLabel = status?.Label ?? "Không xác định",
            ColorClass = status?.ColorClass ?? "bg-secondary",
            CanComment = ev.NewStatusId != TicketStatus.Resolved && 
                         ev.NewStatusId != TicketStatus.Closed && 
                         ev.NewStatusId != TicketStatus.Rejected,
            UpdatedAt = ev.OccurredOn
        };

        await _hubContext.Clients.Group($"ticket_{ev.TicketId}")
            .SendAsync("TicketStatusChanged", payload, cancellationToken);
        await _hubContext.Clients.Group($"Ticket_{ev.TicketId}")
            .SendAsync("TicketStatusChanged", payload, cancellationToken);
    }

    private async Task HandleTicketAssignedAsync(TicketAssignedEvent ev, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.Title, t.CreatorId })
            .FirstOrDefaultAsync(t => t.Id == ev.TicketId, cancellationToken);

        if (ticket == null) return;

        // Thông báo cho Staff được giao việc
        await SendToUserAsync(ev.AssignedToId, 
            $"Bạn được phân công xử lý ticket #{ticket.Id}: {ticket.Title}", 
            $"/Staff/Ticket/Details/{ticket.Id}", cancellationToken);

        // Thông báo cho User tạo ticket
        await SendToUserAsync(ticket.CreatorId, 
            $"Ticket #{ticket.Id} của bạn đã được phân công cho kỹ thuật viên.", 
            $"/User/Ticket/Details/{ticket.Id}", cancellationToken);
    }

    private async Task HandleTicketClosedAsync(TicketClosedEvent ev, CancellationToken cancellationToken)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.AssignedToId })
            .FirstOrDefaultAsync(t => t.Id == ev.TicketId, cancellationToken);

        if (ticket?.AssignedToId != null)
        {
            await SendToUserAsync(ticket.AssignedToId.Value, 
                $"Ticket #{ticket.Id} đã được đánh giá {ev.Rating} sao và hoàn tất đóng.", 
                $"/Staff/Ticket/Details/{ticket.Id}", cancellationToken);
        }
    }
}
