using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Application.Observers;

/// <summary>
/// Observer Pattern: Tự động ghi nhận nhật ký kiểm toán bất biến (Immutable Audit Trail) vào SystemLogs
/// mỗi khi có biến động dữ liệu nghiệp vụ quan trọng trong hệ thống.
/// </summary>
public sealed class SystemLogObserver :
    IDomainEventObserver<TicketCreatedEvent>,
    IDomainEventObserver<TicketAssignedEvent>,
    IDomainEventObserver<TicketStatusChangedEvent>,
    IDomainEventObserver<CommentAddedEvent>,
    IDomainEventObserver<TicketClosedEvent>,
    IDomainEventObserver<UserDeactivatedEvent>
{
    private readonly IItTmsDbContext _dbContext;
    private readonly ICurrentUserService? _currentUserService;

    public SystemLogObserver(
        IItTmsDbContext dbContext,
        ICurrentUserService? currentUserService = null)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task HandleAsync(TicketCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var log = SystemLog.Create(
            action: "CreateTicket",
            details: $"Khởi tạo ticket #{domainEvent.TicketId} '{domainEvent.Title}'. Người tạo: User #{domainEvent.CreatorId}.",
            userId: domainEvent.CreatorId,
            ipAddress: "127.0.0.1"
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(TicketAssignedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var log = SystemLog.Create(
            action: "AssignTicket",
            details: $"Phân công ticket #{domainEvent.TicketId} cho kỹ thuật viên Staff #{domainEvent.AssignedToId} bởi Leader #{domainEvent.LeaderId}.",
            userId: domainEvent.LeaderId,
            ipAddress: "127.0.0.1"
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(TicketStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var log = SystemLog.Create(
            action: "UpdateStatus",
            details: $"Chuyển trạng thái ticket #{domainEvent.TicketId} từ StatusId={domainEvent.OldStatusId} sang StatusId={domainEvent.NewStatusId} bởi User #{domainEvent.ChangedByUserId}.",
            userId: domainEvent.ChangedByUserId,
            ipAddress: "127.0.0.1"
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(CommentAddedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var type = domainEvent.IsInternal ? "Ghi chú nội bộ" : "Bình luận công khai";
        var snippet = domainEvent.Content.Length > 50 ? domainEvent.Content[..50] + "..." : domainEvent.Content;
        var log = SystemLog.Create(
            action: domainEvent.IsInternal ? "AddInternalNote" : "AddComment",
            details: $"Thêm {type} #{domainEvent.CommentId} vào ticket #{domainEvent.TicketId} bởi User #{domainEvent.AuthorUserId}. Nội dung: {snippet}",
            userId: domainEvent.AuthorUserId,
            ipAddress: "127.0.0.1"
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(TicketClosedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var comment = string.IsNullOrWhiteSpace(domainEvent.Comment) ? "Không có ý kiến" : domainEvent.Comment;
        var log = SystemLog.Create(
            action: "CloseTicket",
            details: $"Đóng ticket #{domainEvent.TicketId} bởi User #{domainEvent.ClosedByUserId}. Đánh giá chất lượng: {domainEvent.Rating}/5 sao. Nhận xét: {comment}.",
            userId: domainEvent.ClosedByUserId,
            ipAddress: "127.0.0.1"
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(UserDeactivatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var log = SystemLog.Create(
            action: "DeactivateUser",
            details: $"Vô hiệu hóa tài khoản User #{domainEvent.UserId} bởi Admin #{domainEvent.DeactivatedById}.",
            userId: domainEvent.DeactivatedById,
            ipAddress: "127.0.0.1"
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }
}
