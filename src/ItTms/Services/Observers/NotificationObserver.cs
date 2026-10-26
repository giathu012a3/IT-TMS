



using Microsoft.EntityFrameworkCore;

namespace ItTms.Services;

/// <summary>
/// Observer Pattern: Tự động tạo bản ghi Notification khi các Domain Events phát sinh.
/// Phục vụ Bảng 7.2 TC-03 (Tạo ticket báo Leader), TC-04 (Phân công báo Staff & User),
/// TC-08 (Bình luận), TC-09 (Đóng ticket báo Staff).
/// </summary>
public sealed class NotificationObserver :
    IDomainEventObserver<TicketCreatedEvent>,
    IDomainEventObserver<TicketAssignedEvent>,
    IDomainEventObserver<TicketStatusChangedEvent>,
    IDomainEventObserver<CommentAddedEvent>,
    IDomainEventObserver<TicketClosedEvent>
{
    private readonly IItTmsDbContext _dbContext;

    public NotificationObserver(IItTmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// TC-03: Khi tạo ticket mới -> Tạo Notification cho toàn bộ Leader còn Active.
    /// </summary>
    public async Task HandleAsync(TicketCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var leaderIds = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == UserRole.Leader && u.Status == UserStatus.Active)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        var notifications = new List<Notification>();
        foreach (var leaderId in leaderIds)
        {
            var notif = Notification.Create(
                userId: leaderId,
                message: $"[Yêu cầu mới #{domainEvent.TicketId}] {domainEvent.Title}",
                link: $"/Leader/Ticket/AssignQueue"
            );

            if (notif.IsSuccess)
            {
                notifications.Add(notif.Value);
            }
        }

        if (notifications.Count > 0)
        {
            await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
        }
    }

    /// <summary>
    /// TC-04: Khi phân công ticket -> Tạo thông báo cho Staff và User (người tạo).
    /// </summary>
    public async Task HandleAsync(TicketAssignedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.Title, t.CreatorId })
            .FirstOrDefaultAsync(t => t.Id == domainEvent.TicketId, cancellationToken);

        if (ticket == null) return;

        var notifications = new List<Notification>();

        // 1. Thông báo cho Staff được giao
        var staffNotif = Notification.Create(
            userId: domainEvent.AssignedToId,
            message: $"Bạn được phân công xử lý ticket #{ticket.Id}: {ticket.Title}",
            link: $"/Staff/Ticket/Details/{ticket.Id}"
        );
        if (staffNotif.IsSuccess) notifications.Add(staffNotif.Value);

        // 2. Thông báo cho User tạo ticket
        var userNotif = Notification.Create(
            userId: ticket.CreatorId,
            message: $"Ticket #{ticket.Id} của bạn đã được phân công cho kỹ thuật viên.",
            link: $"/User/Ticket/Details/{ticket.Id}"
        );
        if (userNotif.IsSuccess) notifications.Add(userNotif.Value);

        if (notifications.Count > 0)
        {
            await _dbContext.Notifications.AddRangeAsync(notifications, cancellationToken);
        }
    }

    /// <summary>
    /// Cập nhật tiến độ: Thông báo cho User khi trạng thái thay đổi.
    /// </summary>
    public async Task HandleAsync(TicketStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.NewStatusId == TicketStatus.New || domainEvent.NewStatusId == TicketStatus.Assigned)
            return;

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.Title, t.CreatorId, t.RejectionReason })
            .FirstOrDefaultAsync(t => t.Id == domainEvent.TicketId, cancellationToken);

        if (ticket == null) return;

        string message;
        string link = $"/User/Ticket/Details/{ticket.Id}";

        switch (domainEvent.NewStatusId)
        {
            case TicketStatus.InProgress:
                message = $"Ticket #{ticket.Id} đang được kỹ thuật viên xử lý.";
                break;
            case TicketStatus.Waiting:
                message = $"Ticket #{ticket.Id} cần bạn bổ sung thêm thông tin.";
                break;
            case TicketStatus.Resolved:
                message = $"Ticket #{ticket.Id} đã được xử lý xong. Vui lòng kiểm tra và gửi đánh giá dịch vụ.";
                break;
            case TicketStatus.Rejected:
                message = $"Ticket #{ticket.Id} đã bị từ chối tiếp nhận. Lý do: {ticket.RejectionReason ?? "Không hợp lệ"}";
                break;
            default:
                return;
        }

        var notif = Notification.Create(ticket.CreatorId, message, link);
        if (notif.IsSuccess)
        {
            await _dbContext.Notifications.AddAsync(notif.Value, cancellationToken);
        }
    }

    /// <summary>
    /// TC-08: Thêm bình luận -> Thông báo cho đối tác trao đổi (Internal Note TUYỆT ĐỐI không gửi cho User).
    /// </summary>
    public async Task HandleAsync(CommentAddedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // RÀNG BUỘC BẢO MẬT: Ghi chú nội bộ TUYỆT ĐỐI KHÔNG gửi thông báo cho User
        if (domainEvent.IsInternal)
            return;

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.CreatorId, t.AssignedToId })
            .FirstOrDefaultAsync(t => t.Id == domainEvent.TicketId, cancellationToken);

        if (ticket == null) return;

        Notification? notif = null;

        // Nếu người comment là User -> Thông báo cho Staff phụ trách (nếu đã phân công)
        if (domainEvent.AuthorUserId == ticket.CreatorId && ticket.AssignedToId.HasValue)
        {
            var result = Notification.Create(
                userId: ticket.AssignedToId.Value,
                message: $"Người dùng đã gửi phản hồi mới trên ticket #{ticket.Id}.",
                link: $"/Staff/Ticket/Details/{ticket.Id}"
            );
            if (result.IsSuccess) notif = result.Value;
        }
        // Nếu người comment là Staff/Leader -> Thông báo cho User
        else if (domainEvent.AuthorUserId != ticket.CreatorId)
        {
            var result = Notification.Create(
                userId: ticket.CreatorId,
                message: $"Có tin nhắn mới từ kỹ thuật viên trên ticket #{ticket.Id}.",
                link: $"/User/Ticket/Details/{ticket.Id}"
            );
            if (result.IsSuccess) notif = result.Value;
        }

        if (notif != null)
        {
            await _dbContext.Notifications.AddAsync(notif, cancellationToken);
        }
    }

    /// <summary>
    /// TC-09: Khi User xác nhận đóng và đánh giá ticket -> Thông báo cho Staff phụ trách.
    /// </summary>
    public async Task HandleAsync(TicketClosedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.AssignedToId })
            .FirstOrDefaultAsync(t => t.Id == domainEvent.TicketId, cancellationToken);

        if (ticket?.AssignedToId != null)
        {
            var notif = Notification.Create(
                userId: ticket.AssignedToId.Value,
                message: $"Ticket #{ticket.Id} đã được User đóng và đánh giá {domainEvent.Rating} sao.",
                link: $"/Staff/Ticket/Details/{ticket.Id}"
            );

            if (notif.IsSuccess)
            {
                await _dbContext.Notifications.AddAsync(notif.Value, cancellationToken);
            }
        }
    }
}
