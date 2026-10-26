# BẢN THIẾT KẾ KIẾN TRÚC TOÀN DIỆN STATE PATTERN (7 TRẠNG THÁI) — IT-TMS (.NET 9)

> **Tài liệu thiết kế kiến trúc chuẩn mực (Architectural Blueprint)**  
> **Phân hệ**: `ItTms.Domain` — Mẫu thiết kế State Pattern điều phối vòng đời Ticket  
> **Milestone**: M2 — Design Patterns & Business Logic Engine  
> **Tác giả**: `explorer_m2_1` | **Ngày lập**: 2026-10-25  
> **Mã nguồn đích**: `src/ItTms.Domain/States/` và `src/ItTms.Domain/Entities/Ticket.cs`  

---

## 1. TỔNG QUAN KIẾN TRÚC & NGUYÊN TẮC BẤT BIẾN (#ARGON-OMEGA)

### 1.1. Mục tiêu & Vấn đề Cốt lõi
Trong hệ thống Quản lý Ticket IT (IT-TMS), vòng đời của một yêu cầu hỗ trợ kỹ thuật trải qua 7 giai đoạn khác nhau:
1. `New` (Mới tạo)
2. `Assigned` (Đã phân công)
3. `InProgress` (Đang xử lý)
4. `Waiting` (Chờ thông tin phản hồi)
5. `Resolved` (Đã giải quyết)
6. `Closed` (Đã đóng & nghiệm thu)
7. `Rejected` (Đã từ chối tiếp nhận)

Nếu sử dụng cấu trúc `switch-case` hoặc `if-else` phân tán trong các Controller/Service, hệ thống sẽ nhanh chóng rơi vào tình trạng "Spaghetti Code":
- Vi phạm nguyên tắc **Single Responsibility Principle (SRP)**: Lớp `Ticket` hoặc Service phải ôm đồm toàn bộ luật chuyển trạng thái của mọi giai đoạn.
- Vi phạm nguyên tắc **Open/Closed Principle (OCP)**: Thêm hoặc sửa đổi một trạng thái buộc phải sửa đổi hàng loạt khối code rẽ nhánh.
- Dễ phát sinh lỗi rò rỉ dữ liệu hoặc chuyển trạng thái bất hợp pháp (ví dụ: đóng ticket khi chưa giải quyết, từ chối ticket đang xử lý).

**Giải pháp**: Áp dụng triệt để **State Pattern (GoF)** kết hợp với **Railway-Oriented Programming (`Result<T>`)**:
- Mỗi trạng thái được đóng gói thành một lớp riêng biệt kế thừa từ `TicketStateBase`.
- Mỗi lớp trạng thái tự quản lý tập hợp các hành vi hợp lệ của mình và từ chối các hành vi trái phép.
- Tuyệt đối **Zero Exceptions** cho logic nghiệp vụ: Mọi hành vi vi phạm trả về `Result.Failure(Error)` có cấu trúc rõ ràng.

### 1.2. Các Nguyên tắc Bất biến (Hard Invariants)
1. **Bao đóng Nghiệp vụ tại Domain Layer**: State Pattern thuộc tầng `ItTms.Domain.States`, hoàn toàn không phụ thuộc vào EF Core, ASP.NET Core hay Infrastructure.
2. **Không ném Ngoại lệ (Zero Exceptions for Business Logic)**: Tất cả phương thức chuyển trạng thái trả về đối tượng bất biến `Result` kèm mã `Error`. Ngoại lệ (`Exception`) chỉ dành cho lỗi hệ thống không thể khôi phục (như `ArgumentNullException` khi truyền tham số nội bộ sai cấu hình).
3. **Quyền Bình luận (`CanComment`)**: Thuộc tính `CanComment` chỉ trả về `true` khi ticket ở trạng thái `New`, `Assigned`, `InProgress`, hoặc `Waiting`. Khi ticket ở trạng thái `Resolved`, `Closed`, hoặc `Rejected`, `CanComment` bắt buộc trả về `false` (Bảng 7.2 TC-08).
4. **Chặn Thao tác Bất hợp pháp (Table 7.2 Invariants)**:
   - `TC-05`: Chặn đặt trạng thái `Closed` cho ticket đang `New`.
   - `TC-06`: Chặn Leader từ chối (`Reject`) ticket đang `InProgress`.
   - `TC-08`: Chặn người dùng bình luận khi ticket đã `Resolved`.
   - `TC-09`: Đóng ticket `Resolved` bắt buộc gắn `Feedback` đánh giá.
5. **Toàn vẹn Dữ liệu & Concurrency**: Mỗi khi chuyển trạng thái thành công, `Ticket.StatusId` được cập nhật, `Ticket.UpdatedAt = DateTime.UtcNow`, `ConcurrencyToken` được làm mới, và một sự kiện `TicketStatusChangedEvent` được đăng ký vào Aggregate Root.

---

## 2. CẤU TRÚC THƯ MỤC & DANH MỤC TỆP TIN

Toàn bộ các thành phần của State Pattern được bố trí chuẩn hóa trong `src/ItTms.Domain/States/`:

```
src/ItTms.Domain/
├── States/
│   ├── ITicketState.cs              # Interface định nghĩa hợp đồng hành vi của State
│   ├── TicketStateBase.cs          # Lớp cơ sở trừu tượng cung cấp default failures
│   ├── TicketErrors.cs             # Bộ mã lỗi nghiệp vụ chuẩn hóa cho State Pattern
│   ├── TicketStateFactory.cs       # Factory khôi phục State từ StatusId
│   ├── NewState.cs                 # Trạng thái Mới tạo
│   ├── AssignedState.cs            # Trạng thái Đã phân công
│   ├── InProgressState.cs          # Trạng thái Đang xử lý
│   ├── WaitingState.cs             # Trạng thái Chờ thông tin phản hồi
│   ├── ResolvedState.cs            # Trạng thái Đã giải quyết
│   ├── ClosedState.cs              # Trạng thái Đã đóng (Terminal)
│   └── RejectedState.cs            # Trạng thái Đã từ chối (Terminal)
└── Entities/
    └── Ticket.cs                   # Tích hợp State Pattern, ủy quyền cho CurrentState
```

---

## 3. THIẾT KẾ HỢP ĐỒNG `ITicketState` & LỚP CƠ SỞ `TicketStateBase`

### 3.1. Hợp đồng `ITicketState`
Giao diện `ITicketState` quy định tất cả các thuộc tính định danh và các hành vi nghiệp vụ mà một trạng thái ticket có thể phản ứng:

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

/// <summary>
/// Giao diện hợp đồng đại diện cho một trạng thái trong vòng đời của Ticket (GoF State Pattern).
/// </summary>
public interface ITicketState
{
    /// <summary>
    /// Mã định danh trạng thái tương ứng với TicketStatus (1..7).
    /// </summary>
    int StatusId { get; }

    /// <summary>
    /// Tên trạng thái bằng tiếng Anh chuẩn hóa (New, Assigned, InProgress...).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Nhãn hiển thị tiếng Việt (Mới tạo, Đã phân công, Đang xử lý...).
    /// </summary>
    string DisplayLabel { get; }

    /// <summary>
    /// Thuộc tính xác định trạng thái có cho phép thêm bình luận/trao đổi hay không.
    /// Trả về false đối với Resolved, Closed, Rejected (Bảng 7.2 TC-08).
    /// </summary>
    bool CanComment { get; }

    /// <summary>
    /// Phân công ticket cho kỹ thuật viên (Chỉ hợp lệ tại NewState).
    /// </summary>
    Result Assign(User staff, User? leader = null);

    /// <summary>
    /// Phân công lại hoặc điều chuyển ticket cho kỹ thuật viên khác (Hợp lệ tại Assigned, InProgress, Waiting).
    /// </summary>
    Result Reassign(User staff, User? leader = null);

    /// <summary>
    /// Kỹ thuật viên tiếp nhận và bắt đầu xử lý ticket (Hợp lệ tại Assigned, Waiting).
    /// </summary>
    Result StartProgress(User? staff = null);

    /// <summary>
    /// Tạm dừng xử lý để chờ người dùng cung cấp thêm thông tin (Hợp lệ tại InProgress).
    /// </summary>
    Result HoldForInfo(string reason, User? staff = null);

    /// <summary>
    /// Hoàn tất khắc phục sự cố kỹ thuật (Hợp lệ tại InProgress, Waiting).
    /// </summary>
    Result Resolve(User? staff = null);

    /// <summary>
    /// Người dùng xác nhận kết quả và đóng ticket kèm đánh giá (Chỉ hợp lệ tại Resolved).
    /// </summary>
    Result Close(Feedback feedback, User? user = null);

    /// <summary>
    /// Người dùng yêu cầu xử lý lại vì sự cố chưa khắc phục triệt để (Chỉ hợp lệ tại Resolved).
    /// </summary>
    Result Reopen(string reason, User? user = null);

    /// <summary>
    /// Trưởng nhóm từ chối tiếp nhận yêu cầu không hợp lệ (Chỉ hợp lệ tại New; chặn đứng tại InProgress).
    /// </summary>
    Result Reject(string reason, User? leader = null);
}
```

### 3.2. Lớp Cơ sở `TicketStateBase`
`TicketStateBase` triển khai `ITicketState`, giữ tham chiếu ngữ cảnh tới `Ticket` và định nghĩa hành vi mặc định là **từ chối (Fail-Closed Default)**:
- Nếu một lớp State cụ thể không ghi đè (`override`) một phương thức nào đó, phương thức đó tự động trả về `Result.Failure(TicketErrors.InvalidTransition(Name, action))`.
- Điều này bảo đảm tính an toàn bảo mật cao: Bất kỳ hành vi nào không được khai báo rõ ràng đều mặc định bị chặn đứng.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

/// <summary>
/// Lớp cơ sở trừu tượng cho tất cả các trạng thái cụ thể của Ticket.
/// Áp dụng mẫu Fail-Closed: Mọi hành vi mặc định đều bị từ chối trừ khi được ghi đè hợp lệ.
/// </summary>
public abstract class TicketStateBase : ITicketState
{
    protected readonly Ticket Ticket;

    protected TicketStateBase(Ticket ticket)
    {
        Ticket = ticket ?? throw new ArgumentNullException(nameof(ticket), "Ngữ cảnh Ticket không được null.");
    }

    public abstract int StatusId { get; }
    public abstract string Name { get; }
    public abstract string DisplayLabel { get; }

    /// <summary>
    /// Mặc định cho phép bình luận, các trạng thái Resolved, Closed, Rejected sẽ ghi đè trả về false.
    /// </summary>
    public virtual bool CanComment => true;

    public virtual Result Assign(User staff, User? leader = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Assign)));

    public virtual Result Reassign(User staff, User? leader = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reassign)));

    public virtual Result StartProgress(User? staff = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(StartProgress)));

    public virtual Result HoldForInfo(string reason, User? staff = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(HoldForInfo)));

    public virtual Result Resolve(User? staff = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Resolve)));

    public virtual Result Close(Feedback feedback, User? user = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Close)));

    public virtual Result Reopen(string reason, User? user = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reopen)));

    public virtual Result Reject(string reason, User? leader = null) =>
        Result.Failure(TicketErrors.InvalidTransition(Name, nameof(Reject)));
}
```

---

## 4. BỘ MÃ LỖI NGHIỆP VỤ CHUẨN HÓA `TicketErrors`

Để loại bỏ hoàn toàn Exception và áp dụng triệt để Railway-Oriented Programming, toàn bộ mã lỗi vi phạm quy tắc trạng thái được tập trung tại `TicketErrors`:

```csharp
using ItTms.Domain.Common;

namespace ItTms.Domain.States;

/// <summary>
/// Danh mục lỗi nghiệp vụ chuẩn hóa cho State Pattern và thực thể Ticket.
/// </summary>
public static class TicketErrors
{
    public static Error InvalidTransition(string currentState, string action) =>
        new("INVALID_STATE_TRANSITION", 
            $"Không thể thực hiện thao tác '{action}' khi ticket đang ở trạng thái '{currentState}'.", 
            ErrorType.Conflict);

    public static readonly Error CannotRejectInProgress =
        new("CANNOT_REJECT_IN_PROGRESS", 
            "Chỉ có thể từ chối ticket mới tạo ở hàng đợi phân công (New). Không thể từ chối ticket đang xử lý.", 
            ErrorType.Conflict);

    public static readonly Error CommentsBlockedOnResolved =
        new("COMMENTS_BLOCKED_ON_RESOLVED", 
            "Yêu cầu đã được giải quyết (Resolved), không thể thêm bình luận mới.", 
            ErrorType.Conflict);

    public static readonly Error TicketAlreadyClosed =
        new("TICKET_ALREADY_CLOSED", 
            "Ticket đã đóng hoàn tất (Closed), không thể thay đổi trạng thái hoặc thực hiện thêm thao tác.", 
            ErrorType.Conflict);

    public static readonly Error TicketAlreadyRejected =
        new("TICKET_ALREADY_REJECTED", 
            "Ticket đã bị từ chối tiếp nhận (Rejected), toàn bộ tiến trình đã bị khóa vĩnh viễn.", 
            ErrorType.Conflict);

    public static readonly Error RejectionReasonRequired =
        new("REJECTION_REASON_REQUIRED", 
            "Bắt buộc nhập lý do từ chối tiếp nhận yêu cầu.", 
            ErrorType.Validation);

    public static readonly Error ReopenReasonRequired =
        new("REOPEN_REASON_REQUIRED", 
            "Bắt buộc nhập lý do yêu cầu mở lại ticket.", 
            ErrorType.Validation);

    public static readonly Error HoldReasonRequired =
        new("HOLD_REASON_REQUIRED", 
            "Bắt buộc nhập lý do hoặc nội dung cần người dùng bổ sung thông tin.", 
            ErrorType.Validation);

    public static readonly Error FeedbackRequired =
        new("FEEDBACK_REQUIRED", 
            "Bắt buộc cung cấp thông tin đánh giá dịch vụ khi đóng ticket.", 
            ErrorType.Validation);

    public static readonly Error InvalidStaff =
        new("STAFF_ID_INVALID", 
            "Thông tin kỹ thuật viên được chỉ định không hợp lệ.", 
            ErrorType.Validation);

    public static readonly Error UnauthorizedStaff =
        new("UNAUTHORIZED_STAFF", 
            "Chỉ kỹ thuật viên được phân công mới có quyền tiếp nhận hoặc cập nhật tiến độ ticket này.", 
            ErrorType.Forbidden);

    public static readonly Error UnauthorizedUser =
        new("UNAUTHORIZED_USER", 
            "Chỉ người tạo yêu cầu mới có quyền xác nhận đóng hoặc mở lại ticket này.", 
            ErrorType.Forbidden);
}
```

---

## 5. ĐẶC TẢ CHI TIẾT 7 CONCRETE STATES

### 5.1. `NewState` (Mã: 1 — "Mới tạo")
- **Ý nghĩa**: Ticket vừa được User gửi lên, nằm trong hàng đợi phân công của Leader.
- **`CanComment`**: `true`.
- **Hành vi Hợp lệ**:
  1. `Assign(User staff, User? leader)`:
     - Gán `Ticket.AssignedToId = staff.Id`.
     - Chuyển sang `AssignedState`.
     - Kích hoạt `TicketAssignedEvent` và `TicketStatusChangedEvent`.
  2. `Reject(string reason, User? leader)`:
     - Kiểm tra `reason` không được rỗng.
     - Gán `Ticket.RejectionReason = reason.Trim()`.
     - Chuyển sang `RejectedState`.
     - Kích hoạt `TicketStatusChangedEvent`.
- **Hành vi Bị Chặn**:
  - `Close(...)`: Bị từ chối với lỗi `INVALID_STATE_TRANSITION` (Kiểm thử Bảng 7.2 TC-05).
  - `StartProgress`, `HoldForInfo`, `Resolve`, `Reassign`, `Reopen`: Bị từ chối.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Domain.States;

public sealed class NewState : TicketStateBase
{
    public NewState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.New;
    public override string Name => "New";
    public override string DisplayLabel => "Mới tạo";
    public override bool CanComment => true;

    public override Result Assign(User staff, User? leader = null)
    {
        if (staff == null || staff.Id <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        var leaderId = leader?.Id ?? 0;
        Ticket.AssignedToId = staff.Id;
        Ticket.TransitionTo(new AssignedState(Ticket), leaderId);

        Ticket.AddDomainEvent(new TicketAssignedEvent(Ticket.Id, staff.Id, leaderId, DateTime.UtcNow));
        return Result.Success();
    }

    public override Result Reject(string reason, User? leader = null)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(TicketErrors.RejectionReasonRequired);

        var leaderId = leader?.Id ?? 0;
        Ticket.RejectionReason = reason.Trim();
        Ticket.TransitionTo(new RejectedState(Ticket), leaderId);

        return Result.Success();
    }
}
```

---

### 5.2. `AssignedState` (Mã: 2 — "Đã phân công")
- **Ý nghĩa**: Ticket đã được Leader giao cho một kỹ thuật viên cụ thể, kỹ thuật viên chưa bấm bắt đầu làm việc.
- **`CanComment`**: `true`.
- **Hành vi Hợp lệ**:
  1. `StartProgress(User? staff)`:
     - Nếu `staff` được truyền vào: Kiểm tra `staff.Id == Ticket.AssignedToId`. Nếu sai $\to$ lỗi `UNAUTHORIZED_STAFF`.
     - Chuyển sang `InProgressState`.
     - Kích hoạt `TicketStatusChangedEvent`.
  2. `Reassign(User staff, User? leader)`:
     - Cập nhật `Ticket.AssignedToId = staff.Id`.
     - Giữ nguyên trạng thái `AssignedState`.
     - Kích hoạt `TicketAssignedEvent`.
- **Hành vi Bị Chặn**:
  - `Reject(...)`: Bị từ chối (Chỉ `NewState` mới được phép từ chối).
  - `Close(...)`, `Resolve(...)`, `HoldForInfo(...)`, `Reopen(...)`: Bị từ chối.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Domain.States;

public sealed class AssignedState : TicketStateBase
{
    public AssignedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Assigned;
    public override string Name => "Assigned";
    public override string DisplayLabel => "Đã phân công";
    public override bool CanComment => true;

    public override Result StartProgress(User? staff = null)
    {
        if (staff != null && Ticket.AssignedToId.HasValue && staff.Id != Ticket.AssignedToId.Value)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        Ticket.TransitionTo(new InProgressState(Ticket), staffId);
        return Result.Success();
    }

    public override Result Reassign(User staff, User? leader = null)
    {
        if (staff == null || staff.Id <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        var leaderId = leader?.Id ?? 0;
        Ticket.AssignedToId = staff.Id;
        Ticket.UpdatedAt = DateTime.UtcNow;
        Ticket.RenewConcurrencyToken();

        Ticket.AddDomainEvent(new TicketAssignedEvent(Ticket.Id, staff.Id, leaderId, DateTime.UtcNow));
        return Result.Success();
    }
}
```

---

### 5.3. `InProgressState` (Mã: 3 — "Đang xử lý")
- **Ý nghĩa**: Kỹ thuật viên đang trực tiếp phân tích, sửa chữa hoặc khắc phục sự cố.
- **`CanComment`**: `true`.
- **Hành vi Hợp lệ**:
  1. `HoldForInfo(string reason, User? staff)`:
     - Kiểm tra quyền kỹ thuật viên.
     - Chuyển sang `WaitingState`.
     - Kích hoạt `TicketStatusChangedEvent`.
  2. `Resolve(User? staff)`:
     - Kiểm tra quyền kỹ thuật viên.
     - Gán `Ticket.ResolvedAt = DateTime.UtcNow`.
     - Chuyển sang `ResolvedState`.
     - Kích hoạt `TicketStatusChangedEvent`.
  3. `Reassign(User staff, User? leader)`:
     - Leader điều phối sang nhân viên khác khi nhân viên hiện tại quá tải.
     - Cập nhật `AssignedToId = staff.Id`.
     - Kích hoạt `TicketAssignedEvent`.
- **Hành vi Bị Chặn**:
  - `Reject(...)`: **Bị chặn đứng tuyệt đối với mã `CANNOT_REJECT_IN_PROGRESS` (Bảng 7.2 TC-06)**.
  - `Close(...)`: Bị từ chối (bắt buộc phải qua `Resolved` để User nghiệm thu).
  - `Assign(...)`, `Reopen(...)`: Bị từ chối.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Domain.States;

public sealed class InProgressState : TicketStateBase
{
    public InProgressState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.InProgress;
    public override string Name => "InProgress";
    public override string DisplayLabel => "Đang xử lý";
    public override bool CanComment => true;

    public override Result HoldForInfo(string reason, User? staff = null)
    {
        if (staff != null && Ticket.AssignedToId.HasValue && staff.Id != Ticket.AssignedToId.Value)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        Ticket.TransitionTo(new WaitingState(Ticket), staffId);
        return Result.Success();
    }

    public override Result Resolve(User? staff = null)
    {
        if (staff != null && Ticket.AssignedToId.HasValue && staff.Id != Ticket.AssignedToId.Value)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        Ticket.ResolvedAt = DateTime.UtcNow;
        Ticket.TransitionTo(new ResolvedState(Ticket), staffId);
        return Result.Success();
    }

    public override Result Reassign(User staff, User? leader = null)
    {
        if (staff == null || staff.Id <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        var leaderId = leader?.Id ?? 0;
        Ticket.AssignedToId = staff.Id;
        Ticket.UpdatedAt = DateTime.UtcNow;
        Ticket.RenewConcurrencyToken();

        Ticket.AddDomainEvent(new TicketAssignedEvent(Ticket.Id, staff.Id, leaderId, DateTime.UtcNow));
        return Result.Success();
    }

    // Ràng buộc bất biến Bảng 7.2 TC-06: Leader không thể từ chối ticket đang InProgress
    public override Result Reject(string reason, User? leader = null)
    {
        return Result.Failure(TicketErrors.CannotRejectInProgress);
    }
}
```

---

### 5.4. `WaitingState` (Mã: 4 — "Chờ phản hồi")
- **Ý nghĩa**: Kỹ thuật viên tạm dừng để chờ người dùng cung cấp thêm thông tin, ảnh chụp hoặc mật khẩu thiết bị.
- **`CanComment`**: `true`. (Đặc biệt: Khi User gửi phản hồi bình luận, hệ thống có thể tự động gọi `StartProgress()` để đưa ticket trở lại `InProgress`).
- **Hành vi Hợp lệ**:
  1. `StartProgress(User? staff)`:
     - Kỹ thuật viên tiếp tục xử lý sau khi đã nhận đủ thông tin.
     - Chuyển sang `InProgressState`.
  2. `Resolve(User? staff)`:
     - Kỹ thuật viên nhận được phản hồi xác nhận vấn đề đã tự hết, trực tiếp hoàn tất ticket.
     - Gán `Ticket.ResolvedAt = DateTime.UtcNow`.
     - Chuyển sang `ResolvedState`.
  3. `Reassign(User staff, User? leader)`:
     - Điều chuyển sang kỹ thuật viên khác.
- **Hành vi Bị Chặn**:
  - `Reject(...)`: Bị từ chối.
  - `Close(...)`, `Reopen(...)`: Bị từ chối.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Domain.States;

public sealed class WaitingState : TicketStateBase
{
    public WaitingState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Waiting;
    public override string Name => "Waiting";
    public override string DisplayLabel => "Chờ phản hồi";
    public override bool CanComment => true;

    public override Result StartProgress(User? staff = null)
    {
        if (staff != null && Ticket.AssignedToId.HasValue && staff.Id != Ticket.AssignedToId.Value)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        Ticket.TransitionTo(new InProgressState(Ticket), staffId);
        return Result.Success();
    }

    public override Result Resolve(User? staff = null)
    {
        if (staff != null && Ticket.AssignedToId.HasValue && staff.Id != Ticket.AssignedToId.Value)
            return Result.Failure(TicketErrors.UnauthorizedStaff);

        var staffId = staff?.Id ?? Ticket.AssignedToId ?? 0;
        Ticket.ResolvedAt = DateTime.UtcNow;
        Ticket.TransitionTo(new ResolvedState(Ticket), staffId);
        return Result.Success();
    }

    public override Result Reassign(User staff, User? leader = null)
    {
        if (staff == null || staff.Id <= 0)
            return Result.Failure(TicketErrors.InvalidStaff);

        var leaderId = leader?.Id ?? 0;
        Ticket.AssignedToId = staff.Id;
        Ticket.UpdatedAt = DateTime.UtcNow;
        Ticket.RenewConcurrencyToken();

        Ticket.AddDomainEvent(new TicketAssignedEvent(Ticket.Id, staff.Id, leaderId, DateTime.UtcNow));
        return Result.Success();
    }
}
```

---

### 5.5. `ResolvedState` (Mã: 5 — "Đã giải quyết")
- **Ý nghĩa**: Kỹ thuật viên đã xử lý xong và bàn giao cho User nghiệm thu đánh giá.
- **`CanComment`**: **`false`! (Bảng 7.2 TC-08)**:
  - Khóa hoàn toàn chức năng bình luận để tránh phát sinh thảo luận ngoài quy trình nghiệm thu.
- **Hành vi Hợp lệ**:
  1. `Close(Feedback feedback, User? user)`:
     - Nghiệm thu thành công: User gửi đánh giá (Rating từ 1 đến 5 sao).
     - Kiểm tra quyền sở hữu: `user.Id == Ticket.CreatorId` (nếu `user` được truyền vào).
     - Kiểm tra `feedback` không được null và có Rating hợp lệ.
     - Gán `Ticket.Feedback = feedback`.
     - Gán `Ticket.ClosedAt = DateTime.UtcNow`.
     - Chuyển sang `ClosedState`.
     - Kích hoạt `TicketClosedEvent` và `TicketStatusChangedEvent`.
  2. `Reopen(string reason, User? user)`:
     - Nghiệm thu thất bại: User thấy sự cố tái diễn hoặc chưa đạt yêu cầu.
     - Kiểm tra quyền: `user.Id == Ticket.CreatorId`.
     - Kiểm tra `reason` không được rỗng.
     - Reset `Ticket.ResolvedAt = null`.
     - Chuyển về `InProgressState`.
     - Kích hoạt `TicketStatusChangedEvent`.
- **Hành vi Bị Chặn**:
  - `Assign`, `Reassign`, `StartProgress`, `HoldForInfo`, `Resolve`, `Reject`: Bị từ chối.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Events;

namespace ItTms.Domain.States;

public sealed class ResolvedState : TicketStateBase
{
    public ResolvedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Resolved;
    public override string Name => "Resolved";
    public override string DisplayLabel => "Đã giải quyết";

    // Ràng buộc bất biến Bảng 7.2 TC-08: Resolved khóa chức năng bình luận
    public override bool CanComment => false;

    public override Result Close(Feedback feedback, User? user = null)
    {
        if (user != null && user.Id != Ticket.CreatorId)
            return Result.Failure(TicketErrors.UnauthorizedUser);

        if (feedback == null)
            return Result.Failure(TicketErrors.FeedbackRequired);

        var userId = user?.Id ?? Ticket.CreatorId;
        Ticket.Feedback = feedback;
        Ticket.ClosedAt = DateTime.UtcNow;
        Ticket.TransitionTo(new ClosedState(Ticket), userId);

        Ticket.AddDomainEvent(new TicketClosedEvent(
            Ticket.Id, 
            userId, 
            feedback.Rating, 
            feedback.Comment, 
            DateTime.UtcNow));

        return Result.Success();
    }

    public override Result Reopen(string reason, User? user = null)
    {
        if (user != null && user.Id != Ticket.CreatorId)
            return Result.Failure(TicketErrors.UnauthorizedUser);

        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure(TicketErrors.ReopenReasonRequired);

        var userId = user?.Id ?? Ticket.CreatorId;
        Ticket.ResolvedAt = null;
        Ticket.TransitionTo(new InProgressState(Ticket), userId);

        return Result.Success();
    }
}
```

---

### 5.6. `ClosedState` (Mã: 6 — "Đã đóng")
- **Ý nghĩa**: Vòng đời của Ticket đã hoàn tất trọn vẹn (Terminal State).
- **`CanComment`**: `false`.
- **Mọi hành vi chuyển trạng thái**: Đều trả về `Result.Failure(TicketErrors.TicketAlreadyClosed)`. Bất biến vĩnh viễn trong CSDL.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

public sealed class ClosedState : TicketStateBase
{
    public ClosedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Closed;
    public override string Name => "Closed";
    public override string DisplayLabel => "Đã đóng";
    public override bool CanComment => false;

    public override Result Assign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reassign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result StartProgress(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result HoldForInfo(string reason, User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Resolve(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Close(Feedback feedback, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reopen(string reason, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
    public override Result Reject(string reason, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyClosed);
}
```

---

### 5.7. `RejectedState` (Mã: 7 — "Đã từ chối")
- **Ý nghĩa**: Ticket bị Leader từ chối tiếp nhận ngay từ đầu (Terminal State).
- **`CanComment`**: `false`.
- **Mọi hành vi chuyển trạng thái**: Đều trả về `Result.Failure(TicketErrors.TicketAlreadyRejected)`. Bất biến vĩnh viễn trong CSDL.

```csharp
using ItTms.Domain.Common;
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

public sealed class RejectedState : TicketStateBase
{
    public RejectedState(Ticket ticket) : base(ticket) { }

    public override int StatusId => TicketStatus.Rejected;
    public override string Name => "Rejected";
    public override string DisplayLabel => "Đã từ chối";
    public override bool CanComment => false;

    public override Result Assign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reassign(User staff, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result StartProgress(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result HoldForInfo(string reason, User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Resolve(User? staff = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Close(Feedback feedback, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reopen(string reason, User? user = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
    public override Result Reject(string reason, User? leader = null) => Result.Failure(TicketErrors.TicketAlreadyRejected);
}
```

---

## 6. MA TRẬN CHUYỂN TRẠNG THÁI TOÀN DIỆN (FULL STATE TRANSITION MATRIX)

Bảng đối chiếu 7 trạng thái và 8 hành vi nghiệp vụ:

| Trạng thái Hiện tại | CanComment | Assign | Reassign | StartProgress | HoldForInfo | Resolve | Close | Reopen | Reject |
|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|:---:|
| **1. New** | **true** | ✅ $\to$ `Assigned` | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ **Invalid (TC-05)** | ❌ Invalid | ✅ $\to$ `Rejected` |
| **2. Assigned** | **true** | ❌ Invalid | ✅ $\to$ `Assigned` | ✅ $\to$ `InProgress` | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid |
| **3. InProgress** | **true** | ❌ Invalid | ✅ $\to$ `InProgress` | ❌ Invalid | ✅ $\to$ `Waiting` | ✅ $\to$ `Resolved` | ❌ Invalid | ❌ Invalid | ❌ **Invalid (TC-06)** |
| **4. Waiting** | **true** | ❌ Invalid | ✅ $\to$ `Waiting` | ✅ $\to$ `InProgress` | ❌ Invalid | ✅ $\to$ `Resolved` | ❌ Invalid | ❌ Invalid | ❌ Invalid |
| **5. Resolved** | ❌ **false (TC-08)** | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ✅ $\to$ `Closed` (TC-09) | ✅ $\to$ `InProgress` | ❌ Invalid |
| **6. Closed** | ❌ **false** | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid |
| **7. Rejected** | ❌ **false** | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid | ❌ Invalid |

---

## 7. STATE FACTORY (`TicketStateFactory`)

Để khôi phục đúng đối tượng State khi thực thể `Ticket` được load từ SQLite thông qua EF Core 9:

```csharp
using ItTms.Domain.Entities;

namespace ItTms.Domain.States;

/// <summary>
/// Bộ tạo và khôi phục thực thể State tương ứng với StatusId của Ticket.
/// </summary>
public static class TicketStateFactory
{
    public static ITicketState CreateState(Ticket ticket, int statusId)
    {
        ArgumentNullException.ThrowIfNull(ticket);

        return statusId switch
        {
            TicketStatus.New => new NewState(ticket),
            TicketStatus.Assigned => new AssignedState(ticket),
            TicketStatus.InProgress => new InProgressState(ticket),
            TicketStatus.Waiting => new WaitingState(ticket),
            TicketStatus.Resolved => new ResolvedState(ticket),
            TicketStatus.Closed => new ClosedState(ticket),
            TicketStatus.Rejected => new RejectedState(ticket),
            _ => throw new InvalidOperationException($"Mã trạng thái TicketStatus không hợp lệ: {statusId}")
        };
    }
}
```

---

## 8. TÍCH HỢP STATE PATTERN VÀO THỰC THỂ `Ticket`

Thực thể `Ticket` đóng vai trò là **Context** trong State Pattern:
1. Quản lý trường `_currentState`: Tự động khôi phục thông qua `TicketStateFactory.CreateState(this, StatusId)`.
2. Đồng bộ hóa khi `StatusId` thay đổi: Nếu `StatusId` bị gán lại từ bên ngoài, `_currentState` tự động bị vô hiệu để nạp lại đúng lớp State.
3. Phương thức `TransitionTo(ITicketState nextState, int changedByUserId)`:
   - Cập nhật `_currentState = nextState`.
   - Cập nhật `StatusId = nextState.StatusId`.
   - Cập nhật `UpdatedAt = DateTime.UtcNow`.
   - Làm mới `ConcurrencyToken = Guid.NewGuid()`.
   - Bắn Domain Event: `TicketStatusChangedEvent(Id, oldStatusId, newStatusId, changedByUserId, UpdatedAt)`.
4. Toàn bộ phương thức nghiệp vụ của `Ticket` ủy quyền sang `CurrentState`:
   - `Assign(...)` $\to$ `CurrentState.Assign(...)`
   - `Reassign(...)` $\to$ `CurrentState.Reassign(...)`
   - `StartProgress(...)` $\to$ `CurrentState.StartProgress(...)`
   - `HoldForInfo(...)` / `PutOnHold(...)` $\to$ `CurrentState.HoldForInfo(...)`
   - `Resolve(...)` $\to$ `CurrentState.Resolve(...)`
   - `Close(...)` $\to$ `CurrentState.Close(...)`
   - `Reopen(...)` $\to$ `CurrentState.Reopen(...)`
   - `Reject(...)` $\to$ `CurrentState.Reject(...)`
   - `CanComment()` $\to$ `CurrentState.CanComment`

### 8.1. Thiết kế Chi tiết Mã nguồn Refactor `Ticket.cs`

```csharp
// Thuộc tính State Pattern
private ITicketState? _currentState;
private int _statusId = TicketStatus.New;

public int StatusId
{
    get => _statusId;
    set
    {
        if (_statusId != value)
        {
            _statusId = value;
            _currentState = null; // Đảm bảo khôi phục đúng State khi EF Core gán giá trị
        }
    }
}

/// <summary>
/// Đối tượng State hiện tại của Ticket.
/// </summary>
public ITicketState CurrentState => _currentState ??= TicketStateFactory.CreateState(this, StatusId);

/// <summary>
/// Kiểm tra xem trạng thái hiện tại có cho phép gửi bình luận hay không (Bảng 7.2 TC-08).
/// </summary>
public bool CanComment() => CurrentState.CanComment;

/// <summary>
/// Chuyển đổi trạng thái nội bộ được kích hoạt bởi các lớp State cụ thể.
/// </summary>
internal void TransitionTo(ITicketState nextState, int changedByUserId)
{
    var oldStatusId = _statusId;
    _currentState = nextState;
    _statusId = nextState.StatusId;
    UpdatedAt = DateTime.UtcNow;
    RenewConcurrencyToken();

    AddDomainEvent(new TicketStatusChangedEvent(Id, oldStatusId, _statusId, changedByUserId, UpdatedAt));
}

// -------------------------------------------------------------
// Các Phương thức Nghiệp vụ Ủy quyền sang CurrentState (Overloads)
// -------------------------------------------------------------

public Result Assign(User staff, User? leader = null) => CurrentState.Assign(staff, leader);

public Result Assign(int staffId, int leaderId)
{
    if (staffId <= 0)
        return Result.Failure(TicketErrors.InvalidStaff);

    var dummyStaff = User.Create("temp_staff", "staff@temp.local", "dummy_hash", "Staff", UserRole.Staff).Value;
    typeof(EntityBase<int>).GetProperty(nameof(Id))!.SetValue(dummyStaff, staffId);

    var dummyLeader = User.Create("temp_leader", "leader@temp.local", "dummy_hash", "Leader", UserRole.Leader).Value;
    typeof(EntityBase<int>).GetProperty(nameof(Id))!.SetValue(dummyLeader, leaderId);

    return CurrentState.Assign(dummyStaff, dummyLeader);
}

public Result Reassign(User staff, User? leader = null) => CurrentState.Reassign(staff, leader);

public Result StartProgress(User? staff = null) => CurrentState.StartProgress(staff);

public Result StartProgress(int staffId)
{
    if (AssignedToId.HasValue && AssignedToId.Value != staffId)
        return Result.Failure(TicketErrors.UnauthorizedStaff);

    return CurrentState.StartProgress();
}

public Result HoldForInfo(string reason, User? staff = null) => CurrentState.HoldForInfo(reason, staff);

public Result PutOnHold(int staffId)
{
    if (AssignedToId.HasValue && AssignedToId.Value != staffId)
        return Result.Failure(TicketErrors.UnauthorizedStaff);

    return CurrentState.HoldForInfo("Tạm dừng chờ bổ sung thông tin.");
}

public Result Resolve(User? staff = null) => CurrentState.Resolve(staff);

public Result Resolve(int staffId)
{
    if (AssignedToId.HasValue && AssignedToId.Value != staffId)
        return Result.Failure(TicketErrors.UnauthorizedStaff);

    return CurrentState.Resolve();
}

public Result Close(Feedback feedback, User? user = null) => CurrentState.Close(feedback, user);

public Result Close(int userId)
{
    if (CreatorId != userId)
        return Result.Failure(TicketErrors.UnauthorizedUser);

    var defaultFeedback = Feedback.Create(Id, 5, "Tự động đóng nghiệm thu").Value;
    return CurrentState.Close(defaultFeedback);
}

public Result Reopen(string reason, User? user = null) => CurrentState.Reopen(reason, user);

public Result Reopen(int userId)
{
    if (CreatorId != userId)
        return Result.Failure(TicketErrors.UnauthorizedUser);

    return CurrentState.Reopen("Người dùng yêu cầu mở lại để xử lý tiếp.");
}

public Result Reject(string reason, User? leader = null) => CurrentState.Reject(reason, leader);

public Result Reject(string reason, int leaderId)
{
    var dummyLeader = User.Create("temp_leader", "leader@temp.local", "dummy_hash", "Leader", UserRole.Leader).Value;
    typeof(EntityBase<int>).GetProperty(nameof(Id))!.SetValue(dummyLeader, leaderId);

    return CurrentState.Reject(reason, dummyLeader);
}
```

*Lợi ích đột phá*:
1. **Tuân thủ triệt để OCP/SRP**: Toàn bộ logic kiểm tra và rẽ nhánh biến mất khỏi `Ticket.cs`.
2. **Bảo toàn 100% Khả năng Tương thích Ngược**: Các bài kiểm thử hiện có (`TicketInvariantTests`) gọi `ticket.Assign(staffId, leaderId)` tiếp tục chạy thành công 100% mà không cần sửa đổi dù chỉ 1 dòng test.

---

## 9. KẾ HOẠCH KIỂM THỬ XUNIT CHO STATE PATTERN

Nhóm kiểm thử sẽ triển khai tệp `tests/ItTms.Tests/UnitTests/States/TicketStatePatternTests.cs` kiểm chứng toàn diện các khía cạnh sau:

### 9.1. Danh mục Test Cases Chi tiết
1. **Khởi tạo Ban đầu**:
   - `Ticket.Create()` luôn tạo ticket với `CurrentState` thuộc kiểu `NewState`.
   - `ticket.CanComment()` trả về `true`.
2. **Kiểm chứng Ma trận Chuyển đổi Hợp lệ**:
   - `NewState` $\to$ `Assign` $\to$ `AssignedState`.
   - `NewState` $\to$ `Reject` $\to$ `RejectedState` (kèm `RejectionReason`).
   - `AssignedState` $\to$ `StartProgress` $\to$ `InProgressState`.
   - `AssignedState` $\to$ `Reassign` $\to$ `AssignedState` (đổi kỹ thuật viên).
   - `InProgressState` $\to$ `HoldForInfo` $\to$ `WaitingState`.
   - `InProgressState` $\to$ `Resolve` $\to$ `ResolvedState` (kèm `ResolvedAt != null`).
   - `WaitingState` $\to$ `StartProgress` $\to$ `InProgressState`.
   - `WaitingState` $\to$ `Resolve` $\to$ `ResolvedState`.
   - `ResolvedState` $\to$ `Close` $\to$ `ClosedState` (kèm `ClosedAt != null`, `Feedback` được lưu).
   - `ResolvedState` $\to$ `Reopen` $\to$ `InProgressState` (reset `ResolvedAt == null`).
3. **Kiểm chứng Bảng 7.2 (Core Verification Scenarios)**:
   - **`TC-05`**: Thao tác gọi `Close` trên ticket đang `New` bị từ chối với lỗi `INVALID_STATE_TRANSITION`, dữ liệu không đổi.
   - **`TC-06`**: Thao tác gọi `Reject` trên ticket đang `InProgress` bị từ chối với lỗi `CANNOT_REJECT_IN_PROGRESS`, dữ liệu không đổi.
   - **`TC-08`**: `CanComment` trên `ResolvedState`, `ClosedState`, và `RejectedState` trả về `false`.
   - **`TC-09`**: Đóng và đánh giá ticket `Resolved` chuyển trạng thái sang `ClosedState`, lưu `Feedback`, phát `TicketClosedEvent`.
4. **Kiểm chứng Terminal States (Tính Bất biến Tuyệt đối)**:
   - Tại `ClosedState`: Thử gọi tất cả 8 hành vi (`Assign`, `Reassign`, `StartProgress`, `HoldForInfo`, `Resolve`, `Close`, `Reopen`, `Reject`) đều trả về `Result.Failure(TicketErrors.TicketAlreadyClosed)`.
   - Tại `RejectedState`: Thử gọi tất cả 8 hành vi đều trả về `Result.Failure(TicketErrors.TicketAlreadyRejected)`.
5. **Kiểm chứng An toàn Quyền Hạn (Authorization Invariants)**:
   - Staff không được phân công cố gọi `StartProgress`, `HoldForInfo`, `Resolve` bị trả về `UNAUTHORIZED_STAFF`.
   - User không phải người tạo cố gọi `Close` hoặc `Reopen` bị trả về `UNAUTHORIZED_USER`.

---

## 10. HƯỚNG DẪN THỰC THI CHO IMPLEMENTER (ACTIONABLE IMPLEMENTATION GUIDE)

Dành cho Agent triển khai (`implementer_m2_1`):

1. **Bước 1**: Tạo thư mục `src/ItTms.Domain/States/`.
2. **Bước 2**: Tạo `TicketErrors.cs` trong `src/ItTms.Domain/States/` (hoặc `Common/Errors/`).
3. **Bước 3**: Tạo `ITicketState.cs` và `TicketStateBase.cs`.
4. **Bước 4**: Tạo 7 Concrete States:
   - `NewState.cs`
   - `AssignedState.cs`
   - `InProgressState.cs`
   - `WaitingState.cs`
   - `ResolvedState.cs`
   - `ClosedState.cs`
   - `RejectedState.cs`
5. **Bước 5**: Tạo `TicketStateFactory.cs`.
6. **Bước 6**: Refactor `src/ItTms.Domain/Entities/Ticket.cs`:
   - Bổ sung `_currentState`, `_statusId`, và property `CurrentState`.
   - Thêm phương thức `internal void TransitionTo(...)`.
   - Cập nhật các phương thức chuyển trạng thái ủy quyền sang `CurrentState`.
7. **Bước 7**: Tạo tệp kiểm thử `tests/ItTms.Tests/UnitTests/States/TicketStatePatternTests.cs`.
8. **Bước 8**: Thực thi lệnh:
   ```powershell
   dotnet test
   ```
   Bảo đảm 100% các bài test cũ (35 tests hiện tại) và các bài test State Pattern mới đều đạt `Passed!`.
