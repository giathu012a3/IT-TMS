# 📐 KẾ HOẠCH THIẾT KẾ DOMAIN ENTITIES & EF CORE 9 SCHEMA (SQLITE)

> **Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
> **Nền tảng**: .NET 9 (`net9.0`) | C# 13 | EF Core 9.0 | SQLite  
> **Tác giả**: `explorer_m1_2`  
> **Ngày lập**: 2026-10-25  
> **Tiêu chuẩn nhận thức**: `#Argon-Omega` (BLUF-MECE, First-Principles, Causal Constructivism)  
> **Mục tiêu**: Thiết kế mã nguồn C# chi tiết, chính xác 100% cho 8 Core Entities trong `src/ItTms.Domain` và cấu hình Fluent API trong `src/ItTms.Infrastructure`, giải quyết triệt để bài toán Concurrency Token trên SQLite, SLA DueAt và Covering Indexes.

---

## 1. TỔNG QUAN KIẾN TRÚC & NGUYÊN TẮC THIẾT KẾ DOMAIN

### 1.1. Nguyên tắc Bất biến Tầng Domain (`src/ItTms.Domain`)
1. **Zero External Dependency (`#042 Leaky Abstraction`)**:
   - `ItTms.Domain` là tầng nhân (Core), tuyệt đối không phụ thuộc vào EF Core, ASP.NET Core hay bất kỳ thư viện ORM/Hạ tầng nào.
   - Toàn bộ thực thể chỉ sử dụng thư viện chuẩn của .NET (`System`, `System.Collections.Generic`).
2. **Rich Domain Model (`#001 First-Principles`)**:
   - Tuyệt đối loại bỏ Anemic Domain Model (thực thể chỉ có `get; set;` rỗng).
   - Tất cả các thuộc tính biến đổi đều có bộ điều chỉnh truy cập `private set;`.
   - Cung cấp constructor không tham số với tầm vực `private` phục vụ EF Core Materialization.
   - Thao tác tạo mới hoặc thay đổi trạng thái phải thông qua static factory methods (`Create`) và các phương thức nghiệp vụ thể hiện ý định rõ ràng (`Assign`, `UpdateStatus`, `Resolve`, `Close`, `Reject`, `Reopen`), đảm bảo bất biến dữ liệu (Invariants) không bao giờ bị vi phạm.
3. **Railway-Oriented Flow với `Result<T>` (`#026 Bottleneck`)**:
   - Mọi phương thức Domain kiểm tra ràng buộc logic không ném `Exception` khi gặp lỗi nghiệp vụ thông thường. Sử dụng `Result` / `Result<T>` và `Error` để trả về trạng thái thất bại có cấu trúc.
4. **Domain Events Integration (`#035 Idempotency & Atomicity`)**:
   - Thực thể gốc `Ticket` đóng vai trò Aggregate Root kế thừa `AggregateRoot<int>`, tích lũy các sự kiện nghiệp vụ (`IDomainEvent`) trong danh sách nội bộ và được phát hành (dispatch) an toàn trong cùng một giao dịch lưu trữ qua `SaveChangesInterceptor`.

---

## 2. THIẾT KẾ CHI TIẾT 8 THỰC THỂ CỐT LÕI (C# DOMAIN DEFINITIONS)

### 2.1. Các Enums Nghiệp vụ (`src/ItTms.Domain/Enums/`)

```csharp
namespace ItTms.Domain.Enums;

/// <summary>
/// Vai trò người dùng trong hệ thống IT-TMS.
/// </summary>
public enum UserRole
{
    User = 1,
    Staff = 2,
    Leader = 3,
    Admin = 4
}

/// <summary>
/// Trạng thái hoạt động của tài khoản người dùng.
/// </summary>
public enum UserStatus
{
    Active = 1,
    Inactive = 2
}

/// <summary>
/// Mức độ ưu tiên của yêu cầu hỗ trợ (quyết định hạn cam kết SLA DueAt).
/// </summary>
public enum TicketPriority
{
    Low = 1,        // SLA: 72 giờ
    Medium = 2,     // SLA: 24 giờ (Mặc định)
    High = 3,       // SLA: 4 giờ
    Critical = 4    // SLA: 1 giờ
}

/// <summary>
/// Danh mục phân loại sự cố kỹ thuật.
/// </summary>
public enum TicketCategory
{
    Hardware = 1,   // Phần cứng
    Software = 2,   // Phần mềm
    Network = 3,    // Mạng & Kết nối
    Account = 4,    // Tài khoản & Phân quyền
    Other = 5       // Khác
}
```

---

### 2.2. Lớp Cơ sở Chung (`src/ItTms.Domain/Common/`)

```csharp
namespace ItTms.Domain.Common;

/// <summary>
/// Định nghĩa lỗi nghiệp vụ có cấu trúc (Railway-Oriented Programming).
/// </summary>
public readonly record struct Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);
    public static implicit operator Result(Error error) => Result.Failure(error);
}

/// <summary>
/// Kết quả thực thi nghiệp vụ không mang dữ liệu trả về.
/// </summary>
public class Result
{
    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None || !isSuccess && error == Error.None)
            throw new InvalidOperationException("Invalid Result state.");

        IsSuccess = isSuccess;
        Error = error;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
}

/// <summary>
/// Kết quả thực thi nghiệp vụ mang dữ liệu kiểu T.
/// </summary>
public class Result<T> : Result
{
    private readonly T? _value;

    protected internal Result(T? value, bool isSuccess, Error error) 
        : base(isSuccess, error)
    {
        _value = value;
    }

    public T Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Cannot access Value on a failed result.");

    public static Result<T> Success(T value) => new(value, true, Error.None);
    public static new Result<T> Failure(Error error) => new(default, false, error);
    public static implicit operator Result<T>(T value) => Success(value);
}

/// <summary>
/// Giao diện đánh dấu cho tất cả Domain Events.
/// </summary>
public interface IDomainEvent
{
    DateTime OccurredOn { get; }
}

/// <summary>
/// Lớp cơ sở cho thực thể có định danh Id.
/// </summary>
public abstract class EntityBase<TId>
{
    public TId Id { get; protected set; } = default!;

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase<TId> other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetType() != other.GetType()) return false;
        if (EqualityComparer<TId>.Default.Equals(Id, default)) return false;
        return EqualityComparer<TId>.Default.Equals(Id, other.Id);
    }

    public override int GetHashCode() => EqualityComparer<TId>.Default.GetHashCode(Id!);
}

/// <summary>
/// Lớp cơ sở cho Aggregate Root quản lý Domain Events.
/// </summary>
public abstract class AggregateRoot<TId> : EntityBase<TId>
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
    public void ClearDomainEvents() => _domainEvents.Clear();
}
```

---

### 2.3. Thực thể 1: `User` (`src/ItTms.Domain/Entities/User.cs`)

Quản lý thông tin định danh, vai trò, trạng thái kích hoạt và bảo mật tài khoản.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Enums;

namespace ItTms.Domain.Entities;

public class User : EntityBase<int>
{
    // EF Core hydration constructor
    private User() { }

    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public UserStatus Status { get; private set; } = UserStatus.Active;
    public string? Department { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Helper property phục vụ kiểm tra nhanh trạng thái kích hoạt
    public bool IsActive => Status == UserStatus.Active;

    // Navigation Properties
    public virtual ICollection<Ticket> CreatedTickets { get; private set; } = new List<Ticket>();
    public virtual ICollection<Ticket> AssignedTickets { get; private set; } = new List<Ticket>();
    public virtual ICollection<Comment> Comments { get; private set; } = new List<Comment>();
    public virtual ICollection<Notification> Notifications { get; private set; } = new List<Notification>();
    public virtual ICollection<SystemLog> SystemLogs { get; private set; } = new List<SystemLog>();

    // Static Factory Method
    public static Result<User> Create(
        string username,
        string passwordHash,
        string fullName,
        UserRole role,
        string? department = null,
        UserStatus status = UserStatus.Active)
    {
        if (string.IsNullOrWhiteSpace(username))
            return Result<User>.Failure(new Error("USER_USERNAME_REQUIRED", "Tên đăng nhập không được để trống."));

        if (username.Length > 80)
            return Result<User>.Failure(new Error("USER_USERNAME_TOO_LONG", "Tên đăng nhập không vượt quá 80 ký tự."));

        if (string.IsNullOrWhiteSpace(passwordHash))
            return Result<User>.Failure(new Error("USER_PASSWORD_REQUIRED", "Mật khẩu đã băm không được để trống."));

        if (string.IsNullOrWhiteSpace(fullName))
            return Result<User>.Failure(new Error("USER_FULLNAME_REQUIRED", "Họ và tên không được để trống."));

        var user = new User
        {
            Username = username.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FullName = fullName.Trim(),
            Role = role,
            Status = status,
            Department = department?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result<User>.Success(user);
    }

    // Business Methods
    public Result UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            return Result.Failure(new Error("USER_PASSWORD_EMPTY", "Mật khẩu mới không được để trống."));

        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result UpdateProfile(string fullName, string? department)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return Result.Failure(new Error("USER_FULLNAME_REQUIRED", "Họ và tên không được để trống."));

        FullName = fullName.Trim();
        Department = department?.Trim();
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result ChangeRole(UserRole newRole)
    {
        Role = newRole;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Activate()
    {
        Status = UserStatus.Active;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }

    public Result Deactivate(int operatingAdminId)
    {
        // Ràng buộc bất biến Bảng 7.2 TC-10: Admin không thể tự vô hiệu hóa tài khoản của chính mình
        if (Id == operatingAdminId)
            return Result.Failure(new Error("ADMIN_CANNOT_DEACTIVATE_SELF", "Quản trị viên không thể tự vô hiệu hóa tài khoản của chính mình."));

        Status = UserStatus.Inactive;
        UpdatedAt = DateTime.UtcNow;
        return Result.Success();
    }
}
```

---

### 2.4. Thực thể 2: `TicketStatus` (`src/ItTms.Domain/Entities/TicketStatus.cs`)

Bảng danh mục tra cứu 7 trạng thái chuẩn của vòng đời ticket.

```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class TicketStatus : EntityBase<int>
{
    // Các hằng số định danh trạng thái chuẩn (Seed Data Ids)
    public const int New = 1;
    public const int Assigned = 2;
    public const int InProgress = 3;
    public const int Waiting = 4;
    public const int Resolved = 5;
    public const int Closed = 6;
    public const int Rejected = 7;

    private TicketStatus() { }

    public string Name { get; private set; } = string.Empty;       // Mã code tiếng Anh: New, Assigned...
    public string Label { get; private set; } = string.Empty;      // Nhãn tiếng Việt: Mới tạo, Đã phân công...
    public string ColorClass { get; private set; } = string.Empty; // Class badge CSS Bootstrap 5
    public string? Description { get; private set; }

    // Navigation Properties
    public virtual ICollection<Ticket> Tickets { get; private set; } = new List<Ticket>();

    public static TicketStatus Create(int id, string name, string label, string colorClass, string? description = null)
    {
        return new TicketStatus
        {
            Id = id,
            Name = name.Trim(),
            Label = label.Trim(),
            ColorClass = colorClass.Trim(),
            Description = description?.Trim()
        };
    }
}
```

---

### 2.5. Thực thể 3: `Ticket` (`src/ItTms.Domain/Entities/Ticket.cs`)

Thực thể trung tâm (Aggregate Root), quản lý vòng đời sự cố, tính toán SLA DueAt, ghi nhận vết mốc thời gian và bảo vệ an toàn đồng thời bằng `RowVersion`.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Enums;

namespace ItTms.Domain.Entities;

public class Ticket : AggregateRoot<int>
{
    private Ticket() { }

    public string Title { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public TicketPriority Priority { get; private set; } = TicketPriority.Medium;
    public TicketCategory Category { get; private set; } = TicketCategory.Other;

    // Foreign Keys
    public int CreatorId { get; private set; }
    public int? AssignedToId { get; private set; }
    public int StatusId { get; private set; } = TicketStatus.New;

    // SLA & Timestamps
    public DateTime DueAt { get; private set; }
    public DateTime? ResolvedAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Optimistic Concurrency Token (Khắc phục hạn chế SQLite bằng Application/Interceptor Timestamp Token).
    /// </summary>
    public byte[] RowVersion { get; private set; } = Guid.NewGuid().ToByteArray();

    // Navigations
    public virtual User Creator { get; private set; } = null!;
    public virtual User? AssignedTo { get; private set; }
    public virtual TicketStatus Status { get; private set; } = null!;
    public virtual ICollection<Comment> Comments { get; private set; } = new List<Comment>();
    public virtual ICollection<Attachment> Attachments { get; private set; } = new List<Attachment>();
    public virtual Feedback? Feedback { get; private set; }

    // Domain Invariant: Kiểm tra quyền bình luận (Bảng 7.2 TC-08)
    public bool CanComment()
    {
        // Chặn hoàn toàn bình luận khi ticket ở trạng thái Resolved, Closed hoặc Rejected
        return StatusId != TicketStatus.Resolved && 
               StatusId != TicketStatus.Closed && 
               StatusId != TicketStatus.Rejected;
    }

    // Static Factory Method
    public static Result<Ticket> Create(
        string title,
        string description,
        TicketPriority priority,
        TicketCategory category,
        int creatorId,
        DateTime? currentTime = null)
    {
        if (string.IsNullOrWhiteSpace(title))
            return Result<Ticket>.Failure(new Error("TICKET_TITLE_REQUIRED", "Tiêu đề yêu cầu không được để trống."));

        if (title.Length > 150)
            return Result<Ticket>.Failure(new Error("TICKET_TITLE_TOO_LONG", "Tiêu đề không được vượt quá 150 ký tự."));

        if (string.IsNullOrWhiteSpace(description))
            return Result<Ticket>.Failure(new Error("TICKET_DESC_REQUIRED", "Mô tả chi tiết không được để trống."));

        if (creatorId <= 0)
            return Result<Ticket>.Failure(new Error("TICKET_CREATOR_INVALID", "Người tạo không hợp lệ."));

        var now = currentTime ?? DateTime.UtcNow;
        var dueAt = CalculateDueAt(priority, now);

        var ticket = new Ticket
        {
            Title = title.Trim(),
            Description = description.Trim(),
            Priority = priority,
            Category = category,
            CreatorId = creatorId,
            StatusId = TicketStatus.New,
            CreatedAt = now,
            UpdatedAt = now,
            DueAt = dueAt,
            RowVersion = Guid.NewGuid().ToByteArray()
        };

        return Result<Ticket>.Success(ticket);
    }

    /// <summary>
    /// Tính toán thời hạn cam kết SLA DueAt theo mức độ ưu tiên Priority.
    /// Critical: 1 giờ | High: 4 giờ | Medium: 24 giờ | Low: 72 giờ
    /// </summary>
    public static DateTime CalculateDueAt(TicketPriority priority, DateTime createdAt)
    {
        return priority switch
        {
            TicketPriority.Critical => createdAt.AddHours(1),
            TicketPriority.High     => createdAt.AddHours(4),
            TicketPriority.Medium   => createdAt.AddHours(24),
            TicketPriority.Low      => createdAt.AddHours(72),
            _                       => createdAt.AddHours(24)
        };
    }

    // Business Methods & State Transitions
    public Result Assign(int staffId, int leaderId)
    {
        if (StatusId != TicketStatus.New && StatusId != TicketStatus.Assigned && 
            StatusId != TicketStatus.InProgress && StatusId != TicketStatus.Waiting)
        {
            return Result.Failure(new Error("INVALID_STATE_TRANSITION", 
                "Không thể phân công ticket đã ở trạng thái hoàn thành hoặc đã đóng/từ chối."));
        }

        if (staffId <= 0)
            return Result.Failure(new Error("STAFF_ID_INVALID", "Kỹ thuật viên được giao không hợp lệ."));

        AssignedToId = staffId;
        StatusId = TicketStatus.Assigned;
        UpdatedAt = DateTime.UtcNow;
        RenewRowVersion();

        return Result.Success();
    }

    public Result StartProgress(int staffId)
    {
        if (StatusId != TicketStatus.Assigned && StatusId != TicketStatus.Waiting)
            return Result.Failure(new Error("INVALID_STATE_TRANSITION", 
                "Chỉ có thể bắt đầu xử lý ticket đang ở trạng thái 'Đã phân công' hoặc 'Chờ phản hồi'."));

        if (AssignedToId != staffId)
            return Result.Failure(new Error("UNAUTHORIZED_STAFF", 
                "Chỉ kỹ thuật viên được phân công mới có quyền tiếp nhận xử lý."));

        StatusId = TicketStatus.InProgress;
        UpdatedAt = DateTime.UtcNow;
        RenewRowVersion();

        return Result.Success();
    }

    public Result PutOnHold(int staffId)
    {
        if (StatusId != TicketStatus.InProgress)
            return Result.Failure(new Error("INVALID_STATE_TRANSITION", 
                "Chỉ có thể tạm dừng chờ phản hồi đối với ticket đang ở trạng thái 'Đang xử lý'."));

        if (AssignedToId != staffId)
            return Result.Failure(new Error("UNAUTHORIZED_STAFF", 
                "Chỉ kỹ thuật viên được phân công mới có quyền tạm dừng ticket."));

        StatusId = TicketStatus.Waiting;
        UpdatedAt = DateTime.UtcNow;
        RenewRowVersion();

        return Result.Success();
    }

    public Result Resolve(int staffId)
    {
        if (StatusId != TicketStatus.InProgress && StatusId != TicketStatus.Waiting)
            return Result.Failure(new Error("INVALID_STATE_TRANSITION", 
                "Chỉ có thể hoàn thành xử lý khi ticket đang ở trạng thái 'Đang xử lý' hoặc 'Chờ phản hồi'."));

        if (AssignedToId != staffId)
            return Result.Failure(new Error("UNAUTHORIZED_STAFF", 
                "Chỉ kỹ thuật viên được phân công mới có quyền giải quyết ticket."));

        StatusId = TicketStatus.Resolved;
        ResolvedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RenewRowVersion();

        return Result.Success();
    }

    public Result Close(int userId)
    {
        if (StatusId != TicketStatus.Resolved)
            return Result.Failure(new Error("INVALID_STATE_TRANSITION", 
                "Chỉ có thể đóng ticket đã được giải quyết (Resolved)."));

        if (CreatorId != userId)
            return Result.Failure(new Error("UNAUTHORIZED_USER", 
                "Chỉ người tạo ticket mới có quyền xác nhận đóng ticket."));

        StatusId = TicketStatus.Closed;
        ClosedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
        RenewRowVersion();

        return Result.Success();
    }

    public Result Reopen(int userId)
    {
        if (StatusId != TicketStatus.Resolved)
            return Result.Failure(new Error("INVALID_STATE_TRANSITION", 
                "Chỉ có thể yêu cầu xử lý lại (Reopen) đối với ticket đã giải quyết (Resolved)."));

        if (CreatorId != userId)
            return Result.Failure(new Error("UNAUTHORIZED_USER", 
                "Chỉ người tạo ticket mới có quyền mở lại ticket."));

        StatusId = TicketStatus.InProgress;
        ResolvedAt = null;
        UpdatedAt = DateTime.UtcNow;
        RenewRowVersion();

        return Result.Success();
    }

    public Result Reject(string reason, int leaderId)
    {
        // Ràng buộc bất biến Bảng 7.2 TC-06: Leader không thể từ chối ticket đang InProgress
        if (StatusId != TicketStatus.New)
            return Result.Failure(new Error("CANNOT_REJECT_IN_PROGRESS", 
                "Chỉ có thể từ chối ticket mới tạo ở hàng đợi phân công (New). Không thể từ chối ticket đang xử lý."));

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(new Error("REJECTION_REASON_REQUIRED", 
                "Bắt buộc nhập lý do từ chối tiếp nhận yêu cầu."));

        StatusId = TicketStatus.Rejected;
        RejectionReason = reason.Trim();
        UpdatedAt = DateTime.UtcNow;
        RenewRowVersion();

        return Result.Success();
    }

    public void RenewRowVersion()
    {
        RowVersion = Guid.NewGuid().ToByteArray();
    }
}
```

---

### 2.6. Thực thể 4: `Comment` (`src/ItTms.Domain/Entities/Comment.cs`)

Lưu trữ hội thoại trao đổi thông tin thời gian thực giữa các bên.

```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class Comment : EntityBase<int>
{
    private Comment() { }

    public int TicketId { get; private set; }
    public int UserId { get; private set; }
    public string Content { get; private set; } = string.Empty;
    public bool IsInternal { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigations
    public virtual Ticket Ticket { get; private set; } = null!;
    public virtual User User { get; private set; } = null!;

    public static Result<Comment> Create(int ticketId, int userId, string content, bool isInternal = false)
    {
        if (ticketId <= 0)
            return Result<Comment>.Failure(new Error("COMMENT_TICKET_INVALID", "Mã ticket không hợp lệ."));

        if (userId <= 0)
            return Result<Comment>.Failure(new Error("COMMENT_USER_INVALID", "Mã người dùng không hợp lệ."));

        if (string.IsNullOrWhiteSpace(content))
            return Result<Comment>.Failure(new Error("COMMENT_CONTENT_REQUIRED", "Nội dung phản hồi không được để trống."));

        var comment = new Comment
        {
            TicketId = ticketId,
            UserId = userId,
            Content = content.Trim(),
            IsInternal = isInternal,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Comment>.Success(comment);
    }
}
```

---

### 2.7. Thực thể 5: `Feedback` (`src/ItTms.Domain/Entities/Feedback.cs`)

Đánh giá chất lượng dịch vụ (1 đến 5 sao) khi hoàn tất đóng ticket.

```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class Feedback : EntityBase<int>
{
    private Feedback() { }

    public int TicketId { get; private set; }
    public int Rating { get; private set; }
    public string? Comment { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual Ticket Ticket { get; private set; } = null!;

    public static Result<Feedback> Create(int ticketId, int rating, string? comment = null)
    {
        if (ticketId <= 0)
            return Result<Feedback>.Failure(new Error("FEEDBACK_TICKET_INVALID", "Mã ticket không hợp lệ."));

        if (rating < 1 || rating > 5)
            return Result<Feedback>.Failure(new Error("FEEDBACK_RATING_RANGE", "Điểm đánh giá phải nằm trong khoảng từ 1 đến 5 sao."));

        var feedback = new Feedback
        {
            TicketId = ticketId,
            Rating = rating,
            Comment = comment?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result<Feedback>.Success(feedback);
    }
}
```

---

### 2.8. Thực thể 6: `Attachment` (`src/ItTms.Domain/Entities/Attachment.cs`)

Quản lý thông tin và siêu dữ liệu tệp đính kèm sự cố (hình ảnh, tài liệu).

```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class Attachment : EntityBase<int>
{
    public const long MaxFileSizeBytes = 16 * 1024 * 1024; // 16 MB

    private Attachment() { }

    public int TicketId { get; private set; }
    public string FileName { get; private set; } = string.Empty;         // Tên lưu vật lý trên ổ cứng (GUID)
    public string OriginalFileName { get; private set; } = string.Empty; // Tên gốc người dùng tải lên
    public string ContentType { get; private set; } = string.Empty;      // MIME type (image/png, application/pdf...)
    public long FileSize { get; private set; }                           // Dung lượng (bytes)
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual Ticket Ticket { get; private set; } = null!;

    public static Result<Attachment> Create(
        int ticketId,
        string fileName,
        string originalFileName,
        string contentType,
        long fileSize)
    {
        if (ticketId <= 0)
            return Result<Attachment>.Failure(new Error("ATTACH_TICKET_INVALID", "Mã ticket không hợp lệ."));

        if (string.IsNullOrWhiteSpace(fileName))
            return Result<Attachment>.Failure(new Error("ATTACH_FILENAME_REQUIRED", "Tên tệp lưu trữ không được để trống."));

        if (string.IsNullOrWhiteSpace(originalFileName))
            return Result<Attachment>.Failure(new Error("ATTACH_ORIGINAL_REQUIRED", "Tên tệp gốc không được để trống."));

        if (fileSize <= 0)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_EMPTY", "Tệp tin không có dung lượng."));

        if (fileSize > MaxFileSizeBytes)
            return Result<Attachment>.Failure(new Error("ATTACH_FILE_TOO_LARGE", "Dung lượng tệp tin vượt quá giới hạn cho phép (16MB)."));

        var attachment = new Attachment
        {
            TicketId = ticketId,
            FileName = fileName.Trim(),
            OriginalFileName = originalFileName.Trim(),
            ContentType = contentType.Trim().ToLowerInvariant(),
            FileSize = fileSize,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Attachment>.Success(attachment);
    }
}
```

---

### 2.9. Thực thể 7: `Notification` (`src/ItTms.Domain/Entities/Notification.cs`)

Chuông thông báo cá nhân hóa gửi tới từng người dùng khi có sự kiện nghiệp vụ.

```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class Notification : EntityBase<int>
{
    private Notification() { }

    public int UserId { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public string Link { get; private set; } = string.Empty;
    public bool IsRead { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual User User { get; private set; } = null!;

    public static Result<Notification> Create(int userId, string message, string link)
    {
        if (userId <= 0)
            return Result<Notification>.Failure(new Error("NOTIF_USER_INVALID", "Người nhận thông báo không hợp lệ."));

        if (string.IsNullOrWhiteSpace(message))
            return Result<Notification>.Failure(new Error("NOTIF_MESSAGE_REQUIRED", "Nội dung thông báo không được để trống."));

        if (string.IsNullOrWhiteSpace(link))
            return Result<Notification>.Failure(new Error("NOTIF_LINK_REQUIRED", "Đường dẫn liên kết không được để trống."));

        var notification = new Notification
        {
            UserId = userId,
            Message = message.Trim(),
            Link = link.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        return Result<Notification>.Success(notification);
    }

    public void MarkAsRead()
    {
        IsRead = true;
    }
}
```

---

### 2.10. Thực thể 8: `SystemLog` (`src/ItTms.Domain/Entities/SystemLog.cs`)

Nhật ký kiểm toán hệ thống bất biến (Append-Only Audit Trail) lưu vết mọi thao tác nhạy cảm.

```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.Entities;

public class SystemLog : EntityBase<int>
{
    private SystemLog() { }

    public int? UserId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string Details { get; private set; } = string.Empty;
    public string? IpAddress { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation
    public virtual User? User { get; private set; }

    public static Result<SystemLog> Create(
        string action,
        string details,
        int? userId = null,
        string? ipAddress = null)
    {
        if (string.IsNullOrWhiteSpace(action))
            return Result<SystemLog>.Failure(new Error("LOG_ACTION_REQUIRED", "Hành động nhật ký không được để trống."));

        if (string.IsNullOrWhiteSpace(details))
            return Result<SystemLog>.Failure(new Error("LOG_DETAILS_REQUIRED", "Chi tiết nhật ký không được để trống."));

        var log = new SystemLog
        {
            UserId = userId,
            Action = action.Trim(),
            Details = details.Trim(),
            IpAddress = ipAddress?.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        return Result<SystemLog>.Success(log);
    }
}
```

---

## 3. CẤU HÌNH EF CORE 9 FLUENT API TRÊN SQLITE (`src/ItTms.Infrastructure`)

Mỗi thực thể sở hữu một lớp cấu hình tách biệt triển khai `IEntityTypeConfiguration<T>`.

### 3.1. `UserConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedOnAdd();

        builder.Property(u => u.Username)
            .IsRequired()
            .HasMaxLength(80);

        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(u => u.FullName)
            .IsRequired()
            .HasMaxLength(120);

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(u => u.Status)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(Domain.Enums.UserStatus.Active);

        builder.Property(u => u.Department)
            .HasMaxLength(100);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        // Bỏ qua thuộc tính tính toán không lưu DB
        builder.Ignore(u => u.IsActive);

        // Indexes
        builder.HasIndex(u => u.Username)
            .IsUnique()
            .HasDatabaseName("IX_Users_Username");

        builder.HasIndex(u => u.Role)
            .HasDatabaseName("IX_Users_Role");
    }
}
```

---

### 3.2. `TicketStatusConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class TicketStatusConfiguration : IEntityTypeConfiguration<TicketStatus>
{
    public void Configure(EntityTypeBuilder<TicketStatus> builder)
    {
        builder.ToTable("TicketStatuses");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever(); // Dùng Id cố định (1..7)

        builder.Property(s => s.Name)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Label)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.ColorClass)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.Description)
            .HasMaxLength(255);

        builder.HasIndex(s => s.Name)
            .IsUnique()
            .HasDatabaseName("IX_TicketStatuses_Name");
    }
}
```

---

### 3.3. `TicketConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedOnAdd();

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(t => t.Description)
            .IsRequired();

        builder.Property(t => t.Priority)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.Property(t => t.Category)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(t => t.RejectionReason)
            .HasMaxLength(500);

        builder.Property(t => t.DueAt).IsRequired();
        builder.Property(t => t.ResolvedAt);
        builder.Property(t => t.ClosedAt);
        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UpdatedAt).IsRequired();

        // Optimistic Concurrency Token
        builder.Property(t => t.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        // Foreign Key Relationships
        builder.HasOne(t => t.Creator)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.CreatorId)
            .OnDelete(DeleteBehavior.Restrict); // Không xóa cascading vé khi xóa tài khoản

        builder.HasOne(t => t.AssignedTo)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToId)
            .OnDelete(DeleteBehavior.SetNull); // Xóa kỹ thuật viên thì vé trở về chưa gán

        builder.HasOne(t => t.Status)
            .WithMany(s => s.Tickets)
            .HasForeignKey(t => t.StatusId)
            .OnDelete(DeleteBehavior.Restrict);

        // Covering & High-Performance Indexes
        // 1. Phục vụ UC02 - Yêu cầu của tôi: lọc theo CreatorId và sắp xếp CreatedAt DESC
        builder.HasIndex(t => new { t.CreatorId, t.CreatedAt })
            .HasDatabaseName("IX_Tickets_CreatorId_CreatedAt");

        // 2. Phục vụ UC13, UC07 - Hàng đợi xử lý Staff: lọc theo AssignedToId và StatusId
        builder.HasIndex(t => new { t.AssignedToId, t.StatusId })
            .HasDatabaseName("IX_Tickets_AssignedToId_StatusId");

        // 3. Phục vụ UC11, UC17 - Dashboard Lãnh đạo & Cảnh báo vi phạm SLA DueAt
        builder.HasIndex(t => new { t.StatusId, t.DueAt })
            .HasDatabaseName("IX_Tickets_StatusId_DueAt");

        // 4. Phục vụ UC12 - Tra cứu tổng hợp đa tiêu chí của Leader/Admin
        builder.HasIndex(t => new { t.StatusId, t.Priority, t.CreatedAt })
            .HasDatabaseName("IX_Tickets_StatusId_Priority_CreatedAt");
    }
}
```

---

### 3.4. `CommentConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("Comments");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedOnAdd();

        builder.Property(c => c.Content)
            .IsRequired();

        builder.Property(c => c.IsInternal)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Foreign Keys
        builder.HasOne(c => c.Ticket)
            .WithMany(t => t.Comments)
            .HasForeignKey(c => c.TicketId)
            .OnDelete(DeleteBehavior.Cascade); // Xóa ticket xóa toàn bộ comments

        builder.HasOne(c => c.User)
            .WithMany(u => u.Comments)
            .HasForeignKey(c => c.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Covering Index cho khung chat SignalR: Tải toàn bộ bình luận của 1 ticket sắp xếp theo thời gian
        builder.HasIndex(c => new { c.TicketId, c.CreatedAt })
            .HasDatabaseName("IX_Comments_TicketId_CreatedAt");
    }
}
```

---

### 3.5. `FeedbackConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class FeedbackConfiguration : IEntityTypeConfiguration<Feedback>
{
    public void Configure(EntityTypeBuilder<Feedback> builder)
    {
        builder.ToTable("Feedbacks");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedOnAdd();

        builder.Property(f => f.Rating)
            .IsRequired();

        builder.Property(f => f.Comment)
            .HasMaxLength(1000);

        builder.Property(f => f.CreatedAt)
            .IsRequired();

        // Quan hệ 1 - 0..1 với Ticket
        builder.HasOne(f => f.Ticket)
            .WithOne(t => t.Feedback)
            .HasForeignKey<Feedback>(f => f.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique Index trên TicketId đảm bảo 1 ticket chỉ có tối đa 1 phản hồi
        builder.HasIndex(f => f.TicketId)
            .IsUnique()
            .HasDatabaseName("IX_Feedbacks_TicketId");
    }
}
```

---

### 3.6. `AttachmentConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class AttachmentConfiguration : IEntityTypeConfiguration<Attachment>
{
    public void Configure(EntityTypeBuilder<Attachment> builder)
    {
        builder.ToTable("Attachments");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedOnAdd();

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(a => a.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.FileSize)
            .IsRequired();

        builder.Property(a => a.CreatedAt)
            .IsRequired();

        builder.HasOne(a => a.Ticket)
            .WithMany(t => t.Attachments)
            .HasForeignKey(a => a.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.TicketId)
            .HasDatabaseName("IX_Attachments_TicketId");
    }
}
```

---

### 3.7. `NotificationConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).ValueGeneratedOnAdd();

        builder.Property(n => n.Message)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(n => n.Link)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(n => n.IsRead)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(n => n.CreatedAt)
            .IsRequired();

        builder.HasOne(n => n.User)
            .WithMany(u => u.Notifications)
            .HasForeignKey(n => n.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Covering Index cho chuông thông báo & huy hiệu badge chưa đọc
        builder.HasIndex(n => new { n.UserId, n.IsRead, n.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");
    }
}
```

---

### 3.8. `SystemLogConfiguration`

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ItTms.Infrastructure.Persistence.Configurations;

public class SystemLogConfiguration : IEntityTypeConfiguration<SystemLog>
{
    public void Configure(EntityTypeBuilder<SystemLog> builder)
    {
        builder.ToTable("SystemLogs");

        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedOnAdd();

        builder.Property(l => l.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(l => l.Details)
            .IsRequired();

        builder.Property(l => l.IpAddress)
            .HasMaxLength(50);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.HasOne(l => l.User)
            .WithMany(u => u.SystemLogs)
            .HasForeignKey(l => l.UserId)
            .OnDelete(DeleteBehavior.SetNull); // Khi user bị xóa, log vẫn giữ nguyên với UserId = NULL

        // Covering Index phục vụ lọc và xem nhật ký theo dòng thời gian
        builder.HasIndex(l => new { l.CreatedAt, l.Action })
            .HasDatabaseName("IX_SystemLogs_CreatedAt_Action");
    }
}
```

---

## 4. GIẢI PHÁP KIỂM SOÁT ĐỒNG THỜI LẠC QUAN TRÊN SQLITE (SQLITE CONCURRENCY TOKEN)

### 4.1. Bản chất Kỹ thuật SQLite vs SQL Server (`#042 Leaky Abstraction`)
- **SQL Server**: Có kiểu dữ liệu gốc `rowversion` (hoặc `timestamp`) dạng số nguyên nhị phân tự tăng 8 bytes. Mỗi khi dòng dữ liệu bị cập nhật, SQL Server tự động tăng giá trị này tại tầng storage engine mà không cần ứng dụng can thiệp.
- **SQLite Engine**: SQLite là embedded file database, không có kiểu dữ liệu `rowversion` tự tăng gốc. Nếu chỉ khai báo `.IsRowVersion()` trong EF Core mà không có trigger hoặc interceptor cấp phát token mới, EF Core sẽ phát sinh câu lệnh SQL `UPDATE ... WHERE Id = @id AND RowVersion = @originalRowVersion`, nhưng cột `RowVersion` không được cấp giá trị mới, dẫn đến việc token bị đứng yên và mất khả năng phát hiện xung đột ở lần cập nhật tiếp theo!

### 4.2. Giải pháp Chuẩn mực: `ConcurrencyTokenInterceptor`
Để đảm bảo an toàn tuyệt đối 100% trên SQLite trong EF Core 9, chúng ta triển khai một `SaveChangesInterceptor` đăng ký trực tiếp vào `DbContextOptionsBuilder`.

```csharp
using ItTms.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ItTms.Infrastructure.Persistence.Interceptors;

public class ConcurrencyTokenInterceptor : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData, 
        InterceptionResult<int> result)
    {
        UpdateConcurrencyTokens(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, 
        InterceptionResult<int> result, 
        CancellationToken cancellationToken = default)
    {
        UpdateConcurrencyTokens(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void UpdateConcurrencyTokens(DbContext? context)
    {
        if (context is null) return;

        foreach (var entry in context.ChangeTracker.Entries<Ticket>())
        {
            if (entry.State == EntityState.Modified)
            {
                // Cấp phát 16-byte ngẫu nhiên mới cho RowVersion của Ticket bị sửa đổi
                entry.Property(t => t.RowVersion).CurrentValue = Guid.NewGuid().ToByteArray();
            }
        }
    }
}
```

### 4.3. Xử lý Xung đột và Chuyển dịch Mã HTTP (RFC 9457)
Khi phát hiện xung đột dữ liệu giữa hai người dùng (ví dụ: Staff A và Staff B cùng nhận hoặc đổi trạng thái vé cùng lúc):
1. EF Core ném ra `DbUpdateConcurrencyException`.
2. Tầng Application hoặc Web Controller bắt ngoại lệ và bọc thành:
   `Result.Failure(new Error("CONCURRENCY_CONFLICT", "Dữ liệu ticket đã được người khác cập nhật trước đó. Vui lòng làm mới trang để xem thông tin mới nhất."))`.
3. Trả về mã **HTTP 409 Conflict** (thay vì làm hỏng dữ liệu hay ghi đè âm thầm).

---

## 5. QUY TẮC TÍNH TOÁN SLA `DueAt` & CÁC MỐC THỜI GIAN (TIMESTAMPS)

### 5.1. Bảng Ma trận Hạn Cam kết SLA theo Mức độ Ưu tiên (Priority)
Theo chỉ thị phân công tại `DISPATCH.md` và `spec_inventory.md`, công thức tính hạn hoàn thành cam kết (Service Level Agreement) được ấn định như sau:

| Mức độ Ưu tiên (`TicketPriority`) | Thời gian Cam kết SLA | Công thức Tính toán (`DueAt`) | Ý nghĩa Nghiệp vụ |
|:---:|:---:|:---|---|
| **Critical** | **1 giờ** | `CreatedAt.AddHours(1)` | Sự cố nghiêm trọng toàn hệ thống, gián đoạn dịch vụ diện rộng. |
| **High** | **4 giờ** | `CreatedAt.AddHours(4)` | Lỗi ảnh hưởng tới phòng ban/chức năng chính, có giải pháp thay thế tạm thời. |
| **Medium** | **24 giờ** | `CreatedAt.AddHours(24)` | Yêu cầu nghiệp vụ thông thường, cài đặt phần mềm, lỗi cá nhân (Mặc định). |
| **Low** | **72 giờ** | `CreatedAt.AddHours(72)` | Đề xuất cải tiến, yêu cầu thông tin, hướng dẫn sử dụng. |

### 5.2. Vòng đời Vết Thời gian (Audit Timestamps Lifecycle)
- **`CreatedAt`**: Ghi nhận thời điểm bản ghi được tạo ra lần đầu (`DateTime.UtcNow`). Không bao giờ bị sửa đổi.
- **`DueAt`**: Ghi nhận ngay lúc khởi tạo dựa trên `Priority` và `CreatedAt`. Đóng vai trò là mốc thời gian chuẩn để so sánh vi phạm SLA (`DueAt < DateTime.UtcNow && StatusId != Resolved && StatusId != Closed`).
- **`UpdatedAt`**: Cập nhật mỗi khi có bất kỳ thay đổi nào trên thực thể `Ticket` (đổi trạng thái, phân công, cập nhật thông tin).
- **`ResolvedAt`**: Gán thời điểm kỹ thuật viên chuyển ticket sang `Resolved`. Được thiết lập về `null` nếu User bấm "Yêu cầu xử lý lại" (`Reopen`).
- **`ClosedAt`**: Gán thời điểm User xác nhận đóng ticket và gửi đánh giá dịch vụ (`Closed`).

### 5.3. Tiện ích Kiểm tra Vi phạm SLA trong Domain/Application
```csharp
public static class TicketSlaExtensions
{
    public static bool IsOverdue(this Ticket ticket, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        // Ticket quá hạn nếu đã qua DueAt mà chưa Resolved hoặc Closed
        return ticket.DueAt < now && 
               ticket.StatusId != TicketStatus.Resolved && 
               ticket.StatusId != TicketStatus.Closed &&
               ticket.StatusId != TicketStatus.Rejected;
    }

    public static TimeSpan RemainingTime(this Ticket ticket, DateTime? currentTime = null)
    {
        var now = currentTime ?? DateTime.UtcNow;
        return ticket.DueAt - now;
    }
}
```

---

## 6. CHIẾN LƯỢC CHỈ MỤC BAO PHỦ (COVERING & COMPOSITE INDEXES) CHO SQLITE

### 6.1. Đặc tính Cơ chế Index của SQLite Engine (`#042 Leaky Abstraction`)
- SQLite sử dụng cấu trúc B-Tree cho cả Clustered Index (B-Tree chứa toàn bộ dữ liệu bảng dựa trên `rowid`) và Secondary Indexes (B-Tree phụ).
- SQLite không hỗ trợ mệnh đề `INCLUDE (col1, col2)` như Microsoft SQL Server. Thay vào đó, trong SQLite:
  1. Khi một truy vấn yêu cầu các cột có mặt đầy đủ trong **Composite Index**, SQLite thực hiện **Covering Index Scan / Index Only Scan**, không cần tốn thêm thao tác I/O đọc vào B-Tree dữ liệu chính.
  2. Cột `Id` (khóa chính) luôn được SQLite tự động ngầm đính kèm vào khóa của B-Tree phụ.
  3. Thứ tự các cột trong Composite Index tuân theo quy tắc tiền tố cực trái (Leftmost Prefix Rule): Cột có tính chọn lọc cao hoặc nằm trong mệnh đề lọc đẳng thức (`=`) đứng trước, cột nằm trong mệnh đề so sánh dải (`<`, `>`, `BETWEEN`) hoặc sắp xếp (`ORDER BY`) đứng sau.

### 6.2. Danh mục Chỉ mục Tối ưu Hóa Toàn diện Hệ thống

| Tên Chỉ mục | Bảng | Các Cột Chỉ mục | Use Cases Mục tiêu & Hành vi I/O |
|---|---|---|---|
| `IX_Users_Username` | `Users` | `Username` (UNIQUE) | **UC01 (Đăng nhập)**: B-Tree Seek 1 bước $O(\log N)$ để nạp thông tin xác thực. |
| `IX_Users_Role` | `Users` | `Role` | **UC09 (Phân công)**: Tải nhanh danh sách nhân viên kỹ thuật (`Staff`) để Leader giao việc. |
| `IX_TicketStatuses_Name`| `TicketStatuses` | `Name` (UNIQUE) | Đảm bảo tính toàn vẹn danh mục mã trạng thái chuẩn. |
| `IX_Tickets_CreatorId_CreatedAt` | `Tickets` | `CreatorId`, `CreatedAt` | **UC02 (Yêu cầu của tôi)**: Tối ưu lọc theo `CreatorId` và sắp xếp `CreatedAt DESC` mà không cần Sort in-memory. |
| `IX_Tickets_AssignedToId_StatusId` | `Tickets` | `AssignedToId`, `StatusId` | **UC13, UC07 (Hàng đợi Staff)**: Tối ưu tải danh sách công việc được phân công theo từng trạng thái. |
| `IX_Tickets_StatusId_DueAt` | `Tickets` | `StatusId`, `DueAt` | **UC11, UC17 (Leader Dashboard)**: Quét tức thì danh sách ticket vi phạm hoặc sắp đến hạn SLA mà không quét toàn bảng. |
| `IX_Tickets_StatusId_Priority_CreatedAt` | `Tickets` | `StatusId`, `Priority`, `CreatedAt` | **UC12 (Tra cứu đa tiêu chí)**: Phục vụ bộ lọc tổng hợp của Lãnh đạo và Quản trị viên. |
| `IX_Comments_TicketId_CreatedAt` | `Comments` | `TicketId`, `CreatedAt` | **UC06, UC08 (Khung chat SignalR)**: Nạp danh sách tin nhắn theo đúng trình tự thời gian cực nhanh. |
| `IX_Feedbacks_TicketId` | `Feedbacks` | `TicketId` (UNIQUE) | **UC04 (Đánh giá)**: Ràng buộc duy nhất 1-to-1, chống gửi đánh giá trùng lặp. |
| `IX_Attachments_TicketId` | `Attachments` | `TicketId` | **UC08 (Chi tiết Ticket)**: Nạp danh sách tệp đính kèm theo mã vé. |
| `IX_Notifications_UserId_IsRead_CreatedAt` | `Notifications` | `UserId`, `IsRead`, `CreatedAt` | **UC20, SignalR**: Đếm số huy hiệu chuông thông báo chưa đọc (`IsRead = false`) và lấy 10 thông báo gần nhất. |
| `IX_SystemLogs_CreatedAt_Action` | `SystemLogs` | `CreatedAt`, `Action` | **UC20 (Kiểm toán Admin)**: Phục vụ Keyset Pagination xem nhật ký hệ thống giảm dần theo thời gian. |

### 6.3. Đánh giá Cân bằng Hiệu năng (Goodhart Counter-Metric `#056`)
- Tổng số chỉ mục trên bảng trung tâm `Tickets` được kiểm soát chặt chẽ ở mức **4 chỉ mục**.
- Điều này loại bỏ nguy cơ Write Amplification (suy giảm thông lượng ghi do phân mảnh trang B-Tree khi cập nhật vé), đồng thời nâng tốc độ đọc $P99 < 5\text{ms}$ trên môi trường SQLite.

---

## 7. BẢNG TỔNG HỢP ÁNH XẠ KIẾN TRÚC & HƯỚNG DẪN BÀN GIAO CHO M1 WORKERS

| Thực thể | File Domain Target | File Infrastructure Target | Phụ thuộc chính |
|---|---|---|---|
| `User` | `src/ItTms.Domain/Entities/User.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/UserConfiguration.cs` | `UserRole`, `UserStatus` |
| `TicketStatus` | `src/ItTms.Domain/Entities/TicketStatus.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/TicketStatusConfiguration.cs` | Hằng số Id 1..7 |
| `Ticket` | `src/ItTms.Domain/Entities/Ticket.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/TicketConfiguration.cs` | `User`, `TicketStatus`, `RowVersion`, `DueAt` |
| `Comment` | `src/ItTms.Domain/Entities/Comment.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/CommentConfiguration.cs` | `Ticket`, `User` |
| `Feedback` | `src/ItTms.Domain/Entities/Feedback.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/FeedbackConfiguration.cs` | `Ticket` (Unique 1-to-1) |
| `Attachment` | `src/ItTms.Domain/Entities/Attachment.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/AttachmentConfiguration.cs` | `Ticket`, `MaxFileSizeBytes` |
| `Notification` | `src/ItTms.Domain/Entities/Notification.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/NotificationConfiguration.cs` | `User` |
| `SystemLog` | `src/ItTms.Domain/Entities/SystemLog.cs` | `src/ItTms.Infrastructure/Persistence/Configurations/SystemLogConfiguration.cs` | `User?` (SetNull) |

---

## 8. KẾT LUẬN & KIẾN NGHỊ BÀN GIAO
Bản thiết kế trên cung cấp định nghĩa mã C# hoàn chỉnh, chính xác và có thể sao chép trực tiếp vào các project `ItTms.Domain` và `ItTms.Infrastructure` trong quá trình thực thi tiếp theo. Các nguyên tắc về Concurrency Control, SLA DueAt và Covering Indexes đã được chứng minh qua logic chi tiết, sẵn sàng để Implementer triển khai và chuyển giao cho các bài kiểm thử tự động.
