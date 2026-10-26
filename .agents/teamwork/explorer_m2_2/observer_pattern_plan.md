# 📡 BẢN THIẾT KẾ KIẾN TRÚC OBSERVER PATTERN & DOMAIN EVENTS PIPELINE
> **Dự án**: IT Ticket Management System (.NET 9 Clean Architecture) — IT-TMS  
> **Tác giả**: `explorer_m2_2` | **Phiên bản**: 1.0 (Production-Grade)  
> **Nguyên tắc thiết kế**: Áp dụng chuẩn `#Argon-Omega` (BLUF-MECE, Zero-Fluff, Causal Constructivism)  
> **Mục tiêu**: Tách biệt triệt để logic nghiệp vụ cốt lõi (Domain Model) khỏi các tác vụ phụ trợ (Thông báo đa kênh, Ghi nhật ký kiểm toán bất biến, Đẩy dữ liệu thời gian thực qua SignalR WebSocket).

---

## 1. TỔNG QUAN KIẾN TRÚC & NGUYÊN TẮC BẤT BIẾN (BLUF)

### 1.1. Bottom Line Up Front (BLUF)
Hệ thống IT-TMS áp dụng mô hình **Observer Pattern kết hợp Domain-Driven Design (DDD) Domain Events**:
1. **Subject (Chủ thể phát sinh sự kiện)**: Các thực thể nghiệp vụ (`Ticket`, `User`, `Comment`) kế thừa `AggregateRoot<TId>` hoặc `EntityBase<TId>`. Khi có đột biến trạng thái hợp lệ, thực thể tự đóng gói và lưu trữ một hoặc nhiều `IDomainEvent` vào danh sách nội bộ `_domainEvents`.
2. **Event Dispatcher (Bộ điều phối)**: Bộ điều phối hai pha (`Two-Phase Domain Event Dispatcher`) thu thập toàn bộ sự kiện từ ChangeTracker của EF Core 9:
   - **Pha 1 (In-Transaction Stage)**: Kích hoạt các Observer ghi nhận dữ liệu (`SystemLogObserver`, `NotificationObserver`), thêm các bản ghi `SystemLog` và `Notification` vào cùng `ItTmsDbContext`. Toàn bộ đột biến được commit trong **một Database Transaction duy nhất** (Đảm bảo tính nguyên tử ACID).
   - **Pha 2 (Post-Commit Real-Time Broadcast)**: Chỉ khi `SaveChangesAsync` trả về thành công, hệ thống mới kích hoạt `INotificationDispatcher` phát sóng WebSocket qua SignalR Hub (`/hubs/ticket`), triệt tiêu 100% hiện tượng "Ghost Notification" (thông báo ảo khi DB lỗi rollback).
3. **Observers (Các bên quan sát)**:
   - `NotificationObserver`: Tự động tạo và điều phối thông báo cho User, Staff, Leader dựa trên ngữ cảnh sự kiện.
   - `SystemLogObserver`: Ghi vết kiểm toán bất biến (Audit Trail) cho mọi biến động trạng thái, phân công, bình luận, đóng ticket và vô hiệu hóa tài khoản.

---

### 1.2. Sơ đồ Luồng Thực thi Hai Pha (Two-Phase Dispatch Sequence)

```text
[ Controller / Handler ]
         │
         │ 1. ticket.Assign(staffId, leaderId)
         ▼
┌──────────────────┐
│   Ticket Entity  │ ──► Sinh TicketAssignedEvent & TicketStatusChangedEvent
└──────────────────┘     Lưu vào _domainEvents nội bộ
         │
         │ 2. dbContext.SaveChangesAsync()
         ▼
┌────────────────────────────────────────────────────────────────────────┐
│                   IDomainEventDispatcher (Pha 1)                       │
│  ChangeTracker thu thập events từ IHasDomainEvents & ClearDomainEvents │
└────────────────────────────────────────────────────────────────────────┘
         ├──► NotificationObserver: Tạo Notification (Staff & User) ──┐
         └──► SystemLogObserver: Tạo SystemLog (Audit Trail) ─────────┤
                                                                      ▼
                                                       [ Nạp vào DbContext ]
                                                                      │
                                                       3. SQLite Transaction Commit
                                                                      │
                                                       (Thành công 100% ACID)
                                                                      │
┌─────────────────────────────────────────────────────────────────────▼──┐
│                   IDomainEventDispatcher (Pha 2)                       │
└────────────────────────────────────────────────────────────────────────┘
         │
         │ 4. DispatchPostCommitAsync()
         ▼
┌───────────────────────────────────────┐
│     INotificationDispatcher (SignalR) │
└───────────────────────────────────────┘
         ├──► Clients.User(staffId).ReceiveNotification(...)
         ├──► Clients.User(creatorId).ReceiveNotification(...)
         └──► Clients.Group("ticket_12").TicketStatusUpdated(...)
```

---

## 2. ĐẶC TẢ CHI TIẾT DOMAIN EVENTS (`src/ItTms.Domain/Events/`)

Tất cả các sự kiện kế thừa giao diện bất biến `IDomainEvent` trong `src/ItTms.Domain/Common/IDomainEvent.cs`.

### 2.1. Cập nhật `IDomainEvent.cs`
```csharp
namespace ItTms.Domain.Common;

/// <summary>
/// Giao diện bất biến đại diện cho một Domain Event trong hệ thống.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Thời điểm phát sinh sự kiện theo chuẩn UTC.
    /// </summary>
    DateTime OccurredOn { get; }
}
```

---

### 2.2. Danh mục 6 Domain Events Cốt lõi

#### 1. `TicketCreatedEvent.cs`
Phát sinh khi người dùng tạo mới một yêu cầu hỗ trợ (UC03 / Bảng 7.2 TC-03).
```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Enums;

namespace ItTms.Domain.Events;

/// <summary>
/// Phát sinh khi một Ticket mới được tạo ở trạng thái New.
/// </summary>
public sealed record TicketCreatedEvent(
    int TicketId,
    string Title,
    int CreatorId,
    TicketPriority Priority,
    TicketCategory Category,
    DateTime DueAt,
    DateTime OccurredOn
) : IDomainEvent;
```

#### 2. `TicketAssignedEvent.cs`
Phát sinh khi Leader phân công ticket cho kỹ thuật viên hoặc Leader tự tiếp nhận xử lý (UC09 / Bảng 7.2 TC-04).
```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

/// <summary>
/// Phát sinh khi Ticket được phân công cho Staff kỹ thuật viên.
/// </summary>
public sealed record TicketAssignedEvent(
    int TicketId,
    int StaffId,
    int AssignedById,
    DateTime OccurredOn
) : IDomainEvent;
```

#### 3. `TicketStatusChangedEvent.cs`
Phát sinh mỗi khi trạng thái của ticket thay đổi (`New` $\to$ `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`).
```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

/// <summary>
/// Phát sinh khi Ticket chuyển đổi trạng thái vòng đời.
/// Bổ sung trường Reason phục vụ ghi nhận lý do từ chối (TC-06) hoặc lý do tạm dừng/mở lại.
/// </summary>
public sealed record TicketStatusChangedEvent(
    int TicketId,
    int OldStatusId,
    int NewStatusId,
    int ChangedById,
    string? Reason,
    DateTime OccurredOn
) : IDomainEvent;
```

#### 4. `TicketCommentAddedEvent.cs` (hoặc `CommentAddedEvent.cs`)
Phát sinh khi người dùng hoặc kỹ thuật viên gửi bình luận/ghi chú nội bộ trên ticket (UC06 / Bảng 7.2 TC-08).
```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

/// <summary>
/// Phát sinh khi một bình luận hoặc ghi chú nội bộ được thêm vào Ticket.
/// </summary>
public sealed record TicketCommentAddedEvent(
    int TicketId,
    int CommentId,
    int AuthorId,
    string ContentSnippet,
    bool IsInternal,
    DateTime OccurredOn
) : IDomainEvent;
```

#### 5. `TicketClosedEvent.cs`
Phát sinh khi User xác nhận hoàn tất giải quyết sự cố và gửi đánh giá dịch vụ (UC04, UC19 / Bảng 7.2 TC-09).
```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

/// <summary>
/// Phát sinh khi Ticket chuyển sang trạng thái Closed kèm theo đánh giá sao từ User.
/// </summary>
public sealed record TicketClosedEvent(
    int TicketId,
    int ClosedById,
    int Rating,
    string? FeedbackComment,
    DateTime OccurredOn
) : IDomainEvent;
```

#### 6. `UserDeactivatedEvent.cs`
Phát sinh khi Admin vô hiệu hóa tài khoản người dùng khác (UC15 / Bảng 7.2 TC-10).
```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Events;

/// <summary>
/// Phát sinh khi một tài khoản người dùng bị Admin chuyển trạng thái sang Inactive.
/// </summary>
public sealed record UserDeactivatedEvent(
    int UserId,
    int DeactivatedById,
    string? Reason,
    DateTime OccurredOn
) : IDomainEvent;
```

---

## 3. THIẾT KẾ CƠ CHẾ THU THẬP SỰ KIỆN (EVENT HARVESTING MECHANISM)

### 3.1. Hợp đồng Giao diện `IHasDomainEvents` (`src/ItTms.Domain/Common/IHasDomainEvents.cs`)
Để đảm bảo mọi Entity hoặc AggregateRoot đều có thể tích lũy Domain Events một cách nhất quán:
```csharp
namespace ItTms.Domain.Common;

/// <summary>
/// Giao diện cho các thực thể có khả năng phát sinh Domain Events.
/// </summary>
public interface IHasDomainEvents
{
    IReadOnlyCollection<IDomainEvent> DomainEvents { get; }
    void AddDomainEvent(IDomainEvent domainEvent);
    void RemoveDomainEvent(IDomainEvent domainEvent);
    void ClearDomainEvents();
}
```

### 3.2. Cập nhật `AggregateRoot<TId>` và `EntityBase<TId>`
- `AggregateRoot<TId>` kế thừa `EntityBase<TId>` và triển khai `IHasDomainEvents`.
- `Ticket` kế thừa `AggregateRoot<int>`.
- `User` chuyển sang kế thừa `AggregateRoot<int>` để phát sinh `UserDeactivatedEvent`.

```csharp
namespace ItTms.Domain.Common;

public abstract class AggregateRoot<TId> : EntityBase<TId>, IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

---

## 4. ĐẶC TẢ CÁC OBSERVERS (`src/ItTms.Application/Observers/`)

Các Observer đóng vai trò là `IDomainEventObserver<TEvent>` hoặc `INotificationHandler<TEvent>` (tương thích MediatR).

### 4.1. Giao diện Trừu tượng Observer
```csharp
using ItTms.Domain.Common;

namespace ItTms.Application.Common.Interfaces;

/// <summary>
/// Định nghĩa Observer xử lý một sự kiện Domain Event cụ thể.
/// </summary>
public interface IDomainEventObserver<in TEvent> where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken cancellationToken = default);
}
```

---

### 4.2. `NotificationObserver` (`src/ItTms.Application/Observers/NotificationObserver.cs`)

`NotificationObserver` chịu trách nhiệm tạo bản ghi thực thể `Notification` trong cơ sở dữ liệu và chuyển tiếp thông tin tới kênh Realtime WebSocket:

```csharp
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Domain.Events;
using Microsoft.EntityFrameworkCore;

namespace ItTms.Application.Observers;

public sealed class NotificationObserver :
    IDomainEventObserver<TicketCreatedEvent>,
    IDomainEventObserver<TicketAssignedEvent>,
    IDomainEventObserver<TicketStatusChangedEvent>,
    IDomainEventObserver<TicketCommentAddedEvent>,
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
                message: $"[Yêu cầu mới #{domainEvent.TicketId}] {domainEvent.Title} (Ưu tiên: {domainEvent.Priority})",
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
            userId: domainEvent.StaffId,
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
    /// Cập nhật tiến độ: Thông báo cho User khi trạng thái thay đổi (InProgress, Waiting, Resolved, Rejected).
    /// </summary>
    public async Task HandleAsync(TicketStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        // Bỏ qua nếu là trạng thái New hoặc Assigned (đã xử lý ở event riêng)
        if (domainEvent.NewStatusId == TicketStatus.New || domainEvent.NewStatusId == TicketStatus.Assigned)
            return;

        var ticket = await _dbContext.Tickets
            .AsNoTracking()
            .Select(t => new { t.Id, t.Title, t.CreatorId, t.AssignedToId })
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
                message = $"Ticket #{ticket.Id} cần bạn bổ sung thêm thông tin. Lý do: {domainEvent.Reason ?? "Cần thêm chi tiết"}";
                break;
            case TicketStatus.Resolved:
                message = $"Ticket #{ticket.Id} đã được xử lý xong. Vui lòng kiểm tra và gửi đánh giá dịch vụ.";
                break;
            case TicketStatus.Rejected:
                message = $"Ticket #{ticket.Id} đã bị từ chối tiếp nhận. Lý do: {domainEvent.Reason ?? "Không hợp lệ"}";
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
    /// TC-08: Thêm bình luận -> Thông báo cho đối tác trao đổi (bảo vệ Internal Note không lộ cho User).
    /// </summary>
    public async Task HandleAsync(TicketCommentAddedEvent domainEvent, CancellationToken cancellationToken = default)
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

        // Nếu người comment là User -> Thông báo cho Staff đang phụ trách (nếu đã phân công)
        if (domainEvent.AuthorId == ticket.CreatorId && ticket.AssignedToId.HasValue)
        {
            var result = Notification.Create(
                userId: ticket.AssignedToId.Value,
                message: $"Người dùng đã gửi phản hồi mới trên ticket #{ticket.Id}.",
                link: $"/Staff/Ticket/Details/{ticket.Id}"
            );
            if (result.IsSuccess) notif = result.Value;
        }
        // Nếu người comment là Staff/Leader -> Thông báo cho User
        else if (domainEvent.AuthorId != ticket.CreatorId)
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
```

---

### 4.3. `SystemLogObserver` (`src/ItTms.Application/Observers/SystemLogObserver.cs`)

`SystemLogObserver` đảm bảo mọi hoạt động nhạy cảm hoặc thay đổi trạng thái đều được lưu vết kiểm toán bất biến (Immutable Audit Trail) vào bảng `SystemLogs`.

```csharp
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Application.Observers;

public sealed class SystemLogObserver :
    IDomainEventObserver<TicketCreatedEvent>,
    IDomainEventObserver<TicketAssignedEvent>,
    IDomainEventObserver<TicketStatusChangedEvent>,
    IDomainEventObserver<TicketCommentAddedEvent>,
    IDomainEventObserver<TicketClosedEvent>,
    IDomainEventObserver<UserDeactivatedEvent>
{
    private readonly IItTmsDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService;

    public SystemLogObserver(
        IItTmsDbContext dbContext,
        ICurrentUserService currentUserService)
    {
        _dbContext = dbContext;
        _currentUserService = currentUserService;
    }

    public async Task HandleAsync(TicketCreatedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var log = SystemLog.Create(
            action: "CreateTicket",
            details: $"Khởi tạo ticket #{domainEvent.TicketId} '{domainEvent.Title}'. Độ ưu tiên: {domainEvent.Priority}, Danh mục: {domainEvent.Category}, Hạn SLA: {domainEvent.DueAt:yyyy-MM-dd HH:mm:ss} UTC.",
            userId: domainEvent.CreatorId,
            ipAddress: _currentUserService.IpAddress
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
            details: $"Phân công ticket #{domainEvent.TicketId} cho kỹ thuật viên Staff #{domainEvent.StaffId} bởi Leader #{domainEvent.AssignedById}.",
            userId: domainEvent.AssignedById,
            ipAddress: _currentUserService.IpAddress
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(TicketStatusChangedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var reasonText = string.IsNullOrWhiteSpace(domainEvent.Reason) ? "Không có" : domainEvent.Reason;
        var log = SystemLog.Create(
            action: "UpdateStatus",
            details: $"Chuyển trạng thái ticket #{domainEvent.TicketId} từ StatusId={domainEvent.OldStatusId} sang StatusId={domainEvent.NewStatusId} bởi User #{domainEvent.ChangedById}. Lý do: {reasonText}.",
            userId: domainEvent.ChangedById,
            ipAddress: _currentUserService.IpAddress
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(TicketCommentAddedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var type = domainEvent.IsInternal ? "Ghi chú nội bộ" : "Bình luận công khai";
        var log = SystemLog.Create(
            action: domainEvent.IsInternal ? "AddInternalNote" : "AddComment",
            details: $"Thêm {type} #{domainEvent.CommentId} vào ticket #{domainEvent.TicketId} bởi User #{domainEvent.AuthorId}. Trích lược: {domainEvent.ContentSnippet}",
            userId: domainEvent.AuthorId,
            ipAddress: _currentUserService.IpAddress
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }

    public async Task HandleAsync(TicketClosedEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var feedback = string.IsNullOrWhiteSpace(domainEvent.FeedbackComment) ? "Không có ý kiến" : domainEvent.FeedbackComment;
        var log = SystemLog.Create(
            action: "CloseTicket",
            details: $"Đóng ticket #{domainEvent.TicketId} bởi User #{domainEvent.ClosedById}. Đánh giá chất lượng: {domainEvent.Rating}/5 sao. Nhận xét: {feedback}.",
            userId: domainEvent.ClosedById,
            ipAddress: _currentUserService.IpAddress
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
            details: $"Vô hiệu hóa tài khoản User #{domainEvent.UserId} bởi Admin #{domainEvent.DeactivatedById}. Lý do: {domainEvent.Reason ?? "Theo chỉ định quản trị"}.",
            userId: domainEvent.DeactivatedById,
            ipAddress: _currentUserService.IpAddress
        );

        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value, cancellationToken);
        }
    }
}
```

---

## 5. BỘ ĐIỀU PHỐI SỰ KIỆN: IN-MEMORY DISPATCHER & MEDIATR DUAL-PATH

Để tối đa hóa tính linh hoạt và tuân thủ nguyên tắc Boring Technology (#050), giải pháp hỗ trợ 2 đường lối:
- **Đường lối 1 (Mặc định)**: `InMemoryDomainEventDispatcher` sử dụng chính ASP.NET Core `IServiceProvider` (Zero External Dependencies, không cần cài MediatR).
- **Đường lối 2 (MediatR Adapter)**: Drop-in tương thích nếu nhóm dự án chọn `dotnet add package MediatR`.

### 5.1. Hợp đồng `IDomainEventDispatcher` (`src/ItTms.Application/Common/Interfaces/IDomainEventDispatcher.cs`)
```csharp
using ItTms.Domain.Common;

namespace ItTms.Application.Common.Interfaces;

/// <summary>
/// Hợp đồng điều phối Domain Events theo 2 pha độc lập.
/// </summary>
public interface IDomainEventDispatcher
{
    /// <summary>
    /// Pha 1: Thực thi các Observer trong cùng Transaction (Staging Notification & SystemLog vào DbContext).
    /// </summary>
    Task DispatchInTransactionAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);

    /// <summary>
    /// Pha 2: Thực thi các tác vụ ngoại vi sau khi Transaction đã commit thành công (SignalR WebSocket Broadcast).
    /// </summary>
    Task DispatchPostCommitAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}
```

---

### 5.2. Hiện thực `InMemoryDomainEventDispatcher` (`src/ItTms.Infrastructure/Events/InMemoryDomainEventDispatcher.cs`)

```csharp
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ItTms.Infrastructure.Events;

public sealed class InMemoryDomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly INotificationDispatcher _realtimeDispatcher;
    private readonly ILogger<InMemoryDomainEventDispatcher> _logger;

    public InMemoryDomainEventDispatcher(
        IServiceProvider serviceProvider,
        INotificationDispatcher realtimeDispatcher,
        ILogger<InMemoryDomainEventDispatcher> logger)
    {
        _serviceProvider = serviceProvider;
        _realtimeDispatcher = realtimeDispatcher;
        _logger = logger;
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
                        var task = (Task)method.Invoke(handler, new object[] { domainEvent, cancellationToken })!;
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
        foreach (var domainEvent in domainEvents)
        {
            try
            {
                // Chuyển tiếp tới SignalR notification dispatcher đẩy thông báo thời gian thực
                await _realtimeDispatcher.DispatchDomainEventAsync(domainEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                // Lỗi SignalR không được phép làm sập tiến trình vì DB đã commit thành công
                _logger.LogWarning(ex, "Không thể phát sóng thời gian thực cho sự kiện {EventType} qua SignalR", domainEvent.GetType().Name);
            }
        }
    }
}
```

---

### 5.3. Tích hợp Tự động vào EF Core `ItTmsDbContext`

Có 2 phương án tích hợp:
- **Phương án A: Ghi đè `SaveChangesAsync` trong `ItTmsDbContext`**: Đơn giản, trực quan, không cần đăng ký thêm Interceptor.
- **Phương án B: EF Core `SaveChangesInterceptor`**: Tách rời hoàn toàn khỏi DbContext.

Dưới đây là mã nguồn chuẩn hóa cho **Phương án A**:
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    // 1. Thu thập toàn bộ Domain Events từ các thực thể được ChangeTracker theo dõi
    var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
        .Where(e => e.Entity.DomainEvents.Count > 0)
        .Select(e => e.Entity)
        .ToList();

    if (entitiesWithEvents.Count == 0)
    {
        return await base.SaveChangesAsync(cancellationToken);
    }

    var domainEvents = entitiesWithEvents
        .SelectMany(e => e.DomainEvents)
        .ToList();

    // 2. Dọn sạch sự kiện trong Entity để tránh lặp vô tận (Prevent Double Dispatch)
    entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

    // 3. Pha 1: Kích hoạt In-Transaction Observers (Ghi SystemLog & Notification)
    // Các bản ghi mới này tự động được ChangeTracker ghi nhận vào cùng Transaction
    if (_domainEventDispatcher != null)
    {
        await _domainEventDispatcher.DispatchInTransactionAsync(domainEvents, cancellationToken);
    }

    // 4. Lưu toàn bộ thay đổi xuống SQLite trong một giao dịch nguyên tử duy nhất
    var result = await base.SaveChangesAsync(cancellationToken);

    // 5. Pha 2: Kích hoạt Post-Commit Handlers (SignalR WebSocket Push)
    if (_domainEventDispatcher != null)
    {
        await _domainEventDispatcher.DispatchPostCommitAsync(domainEvents, cancellationToken);
    }

    return result;
}
```

---

## 6. HỢP ĐỒNG GIAO TIẾP REAL-TIME SIGNALR (`INotificationDispatcher`)

### 6.1. Hợp đồng Interface
```csharp
using ItTms.Domain.Common;

namespace ItTms.Application.Common.Interfaces;

public interface INotificationDispatcher
{
    Task SendToUserAsync(int userId, string message, string link, CancellationToken cancellationToken = default);
    Task SendToRoleAsync(string roleName, string message, string link, CancellationToken cancellationToken = default);
    Task NotifyTicketGroupAsync(int ticketId, string eventName, object payload, CancellationToken cancellationToken = default);
    Task DispatchDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}
```

### 6.2. Hiện thực SignalR `SignalRNotificationDispatcher` (`src/ItTms.Infrastructure/Realtime/`)
```csharp
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Common;
using ItTms.Domain.Events;
using Microsoft.AspNetCore.SignalR;

namespace ItTms.Infrastructure.Realtime;

public sealed class SignalRNotificationDispatcher : INotificationDispatcher
{
    private readonly IHubContext<TicketHub> _hubContext;

    public SignalRNotificationDispatcher(IHubContext<TicketHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToUserAsync(int userId, string message, string link, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
        {
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task SendToRoleAsync(string roleName, string message, string link, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"role_{roleName}").SendAsync("ReceiveNotification", new
        {
            Message = message,
            Link = link,
            CreatedAt = DateTime.UtcNow
        }, cancellationToken);
    }

    public async Task NotifyTicketGroupAsync(int ticketId, string eventName, object payload, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group($"ticket_{ticketId}").SendAsync(eventName, payload, cancellationToken);
    }

    public async Task DispatchDomainEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        switch (domainEvent)
        {
            case TicketCreatedEvent created:
                await SendToRoleAsync("Leader", $"Ticket mới #{created.TicketId}: {created.Title}", $"/Leader/Ticket/AssignQueue", cancellationToken);
                break;

            case TicketAssignedEvent assigned:
                await SendToUserAsync(assigned.StaffId, $"Bạn được phân công ticket #{assigned.TicketId}", $"/Staff/Ticket/Details/{assigned.TicketId}", cancellationToken);
                break;

            case TicketStatusChangedEvent statusChanged:
                await NotifyTicketGroupAsync(statusChanged.TicketId, "TicketStatusUpdated", new
                {
                    TicketId = statusChanged.TicketId,
                    NewStatusId = statusChanged.NewStatusId
                }, cancellationToken);
                break;

            case TicketCommentAddedEvent comment:
                if (!comment.IsInternal)
                {
                    await NotifyTicketGroupAsync(comment.TicketId, "ReceiveComment", new
                    {
                        TicketId = comment.TicketId,
                        CommentId = comment.CommentId,
                        AuthorId = comment.AuthorId,
                        Content = comment.ContentSnippet
                    }, cancellationToken);
                }
                break;
        }
    }
}
```

---

## 7. ĐĂNG KÝ DEPENDENCY INJECTION (IOC CONTAINER)

### 7.1. Cấu hình tại `ItTms.Application/DependencyInjection.cs`
```csharp
using ItTms.Application.Common.Interfaces;
using ItTms.Application.Observers;
using ItTms.Domain.Events;
using Microsoft.Extensions.DependencyInjection;

namespace ItTms.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Đăng ký Observers cho từng loại Domain Event
        services.AddScoped<NotificationObserver>();
        services.AddScoped<SystemLogObserver>();

        // Đăng ký IDomainEventObserver cho TicketCreatedEvent
        services.AddScoped<IDomainEventObserver<TicketCreatedEvent>, NotificationObserver>();
        services.AddScoped<IDomainEventObserver<TicketCreatedEvent>, SystemLogObserver>();

        // Đăng ký IDomainEventObserver cho TicketAssignedEvent
        services.AddScoped<IDomainEventObserver<TicketAssignedEvent>, NotificationObserver>();
        services.AddScoped<IDomainEventObserver<TicketAssignedEvent>, SystemLogObserver>();

        // Đăng ký IDomainEventObserver cho TicketStatusChangedEvent
        services.AddScoped<IDomainEventObserver<TicketStatusChangedEvent>, NotificationObserver>();
        services.AddScoped<IDomainEventObserver<TicketStatusChangedEvent>, SystemLogObserver>();

        // Đăng ký IDomainEventObserver cho TicketCommentAddedEvent
        services.AddScoped<IDomainEventObserver<TicketCommentAddedEvent>, NotificationObserver>();
        services.AddScoped<IDomainEventObserver<TicketCommentAddedEvent>, SystemLogObserver>();

        // Đăng ký IDomainEventObserver cho TicketClosedEvent
        services.AddScoped<IDomainEventObserver<TicketClosedEvent>, NotificationObserver>();
        services.AddScoped<IDomainEventObserver<TicketClosedEvent>, SystemLogObserver>();

        // Đăng ký IDomainEventObserver cho UserDeactivatedEvent
        services.AddScoped<IDomainEventObserver<UserDeactivatedEvent>, SystemLogObserver>();

        return services;
    }
}
```

### 7.2. Cấu hình tại `ItTms.Infrastructure/DependencyInjection.cs`
```csharp
services.AddScoped<IDomainEventDispatcher, InMemoryDomainEventDispatcher>();
services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();
```

---

## 8. MA TRẬN ĐỐI CHIẾU NGHIỆM THU (VERIFICATION MATRIX VỚI BẢNG 7.2)

| Mã Kịch bản | Tên Thao tác | Sự kiện Phát sinh | Hành động của `NotificationObserver` | Hành động của `SystemLogObserver` | Hành động của SignalR (`TicketHub`) |
|:---:|---|---|---|---|---|
| **TC-03** | User tạo Ticket mới | `TicketCreatedEvent` | Quét danh sách Leader Active $\to$ Thêm `Notification` cho mỗi Leader | Tạo bản ghi `SystemLog` (Action = `CreateTicket`) | Phát chuông thông báo tới nhóm `role_Leader` |
| **TC-04** | Leader phân công ticket cho Staff | `TicketAssignedEvent` & `TicketStatusChangedEvent` | Thêm `Notification` cho Staff được giao & User tạo ticket | Tạo bản ghi `SystemLog` (Action = `AssignTicket` & `UpdateStatus`) | Đẩy thông báo cá nhân tới Staff và User |
| **TC-06** | Leader thử từ chối ticket InProgress | Bị chặn tại Domain Model (`Result.Failure`) | Không sinh sự kiện $\to$ Không thông báo | Không ghi log thay đổi trạng thái | Không broadcast |
| **TC-08** | User bình luận trên ticket Resolved | Bị chặn bởi `CanComment() == false` | Không sinh sự kiện $\to$ Không thông báo | Không ghi log bình luận | Không broadcast |
| **TC-09** | User đóng và đánh giá ticket | `TicketClosedEvent` & `TicketStatusChangedEvent` | Thêm `Notification` cho Staff phụ trách kèm số sao | Tạo bản ghi `SystemLog` (Action = `CloseTicket`) kèm Rating & Feedback | Đẩy thông báo tới Staff |
| **TC-10** | Admin vô hiệu hóa User khác | `UserDeactivatedEvent` | Không gửi thông báo cho tài khoản bị khóa | Tạo bản ghi `SystemLog` (Action = `DeactivateUser`) | Ngắt quyền truy cập ở request kế tiếp |

---

## 9. KẾ HOẠCH KIỂM THỬ ĐƠN VỊ (UNIT & INTEGRATION TESTS PLAN)

Tập trung vào kiểm thử hành vi Observer và Dispatcher trong `tests/ItTms.Tests`:

### 9.1. Danh mục Test Cases Kiểm thử `NotificationObserver`
1. `NotificationObserver_OnTicketCreated_ShouldCreateNotificationsForActiveLeadersOnly`: Đảm bảo chỉ các Leader có `Status == Active` mới nhận thông báo, bỏ qua Leader `Inactive`.
2. `NotificationObserver_OnTicketAssigned_ShouldNotifyBothStaffAndCreator`: Xác minh cả 2 bản ghi Notification cho Staff và User được tạo chính xác.
3. `NotificationObserver_OnInternalCommentAdded_MustNeverNotifyUser`: Thử nghiệm đối kháng: Khi `IsInternal == true`, assert chắc chắn không có bản ghi Notification nào gửi tới `CreatorId`.
4. `NotificationObserver_OnTicketClosed_ShouldNotifyAssignedStaffWithRating`: Kiểm tra nội dung thông báo chứa số sao đánh giá.

### 9.2. Danh mục Test Cases Kiểm thử `SystemLogObserver`
1. `SystemLogObserver_OnTicketStatusChanged_ShouldRecordImmutableLogWithReason`: Kiểm tra log lưu đúng `OldStatus`, `NewStatus`, `UserId` và `Reason`.
2. `SystemLogObserver_OnUserDeactivated_ShouldRecordAdminAuditLog`: Kiểm tra ghi nhận hành động `DeactivateUser` kèm AdminId.
3. `SystemLogObserver_WhenIpAddressNull_ShouldFallbackToDefaultSafeString`: Thử nghiệm biên khi request không có IP address.

### 9.3. Danh mục Test Cases Kiểm thử `DomainEventDispatcher` & Two-Phase Pipeline
1. `Dispatcher_InTransactionPhase_ShouldStageEntitiesBeforeSave`: Xác minh `SystemLog` và `Notification` được thêm vào ChangeTracker trước khi `base.SaveChangesAsync` được gọi.
2. `Dispatcher_WhenDatabaseFails_MustNeverTriggerPostCommitSignalR`: Thử nghiệm đối kháng: Giả lập SQLite ném `DbUpdateConcurrencyException`, assert không có bất kỳ lời gọi nào tới `_realtimeDispatcher`.
3. `Dispatcher_DrainQueue_ShouldPreventDoubleDispatch`: Đảm bảo sau khi dispatch, `entity.DomainEvents` rỗng hoàn toàn (`Count == 0`).
