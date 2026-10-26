# HANDOFF REPORT: OBSERVER PATTERN & DOMAIN EVENTS PIPELINE BLUEPRINT
> **Agent**: `explorer_m2_2`  
> **Milestone**: M2 - Design Patterns & Business Logic Engine  
> **Handoff Type**: Hard (Task Complete)  
> **Timestamp**: 2026-10-25T09:10:00Z  

---

## 1. OBSERVATION

1. **Cấu trúc thực thể và Domain Events hiện tại**:
   - `src/ItTms.Domain/Common/IDomainEvent.cs` (lines 6-9):
     ```csharp
     public interface IDomainEvent
     {
         DateTime OccurredOn { get; }
     }
     ```
   - `src/ItTms.Domain/Common/AggregateRoot.cs` (lines 6-14): Đã định nghĩa `_domainEvents`, `DomainEvents`, `AddDomainEvent`, `ClearDomainEvents`.
   - `src/ItTms.Domain/Entities/Ticket.cs` (lines 7, 92, 132-133, 153, 173, 194, 215, 236, 258): `Ticket` kế thừa `AggregateRoot<int>` và phát sinh các sự kiện `TicketCreatedEvent`, `TicketAssignedEvent`, `TicketStatusChangedEvent`.
   - `src/ItTms.Domain/Entities/User.cs` (lines 7, 130-139): `User` kế thừa `EntityBase<int>`. Phương thức `Deactivate` chưa phát sinh `UserDeactivatedEvent`.
   - Thư mục `src/ItTms.Domain/Events/` có 5 files: `TicketCreatedEvent.cs`, `TicketAssignedEvent.cs`, `TicketStatusChangedEvent.cs`, `TicketClosedEvent.cs`, `CommentAddedEvent.cs`.
     - `TicketStatusChangedEvent.cs` thiếu tham số `Reason` để truyền lý do từ chối (TC-06) hoặc lý do tạm dừng/mở lại.
     - `TicketCreatedEvent.cs` thiếu `Priority` và `Category` trong event record.

2. **Hiện trạng tầng Application & Infrastructure**:
   - `src/ItTms.Application/` hiện chỉ có thư mục `Common/Interfaces/` và `DependencyInjection.cs`. Chưa có thư mục `src/ItTms.Application/Observers/`.
   - Solution không có package MediatR (tệp `src/ItTms.Application/ItTms.Application.csproj` chỉ tham chiếu `Microsoft.EntityFrameworkCore` 9.0.2 và `Microsoft.Extensions.DependencyInjection.Abstractions` 9.0.2).
   - `src/ItTms.Infrastructure/Persistence/ItTmsDbContext.cs` kế thừa `DbContext` nhưng chưa ghi đè `SaveChangesAsync` để thu thập và kích hoạt dispatch Domain Events.

3. **Yêu cầu Bảng 7.2 từ tài liệu đặc tả `spec_inventory.md`**:
   - **TC-03**: User tạo ticket mới -> Sinh thông báo cho toàn bộ Leader còn Active; phát chuông qua SignalR; ghi `SystemLog`.
   - **TC-04**: Leader phân công ticket cho Staff -> Sinh thông báo cho Staff và User; phát SignalR.
   - **TC-08**: Bình luận trên ticket -> Chặn khi ticket ở `Resolved`, `Closed`, `Rejected`; không gửi thông báo cho User nếu là ghi chú nội bộ (`IsInternal == true`).
   - **TC-09**: User đóng và đánh giá ticket -> Sinh thông báo cho Staff phụ trách; ghi `SystemLog` kèm số sao.
   - **TC-10**: Admin vô hiệu hóa tài khoản -> Ghi `SystemLog` kiểm toán; chặn tự vô hiệu hóa chính mình.

---

## 2. LOGIC CHAIN

1. **Từ Observation 1 & 2 $\to$ Quyết định thiết kế Domain Events & `IHasDomainEvents`**:
   - `IDomainEvent` hiện tại đã có `OccurredOn`. Các record event cần bổ sung các trường ngữ cảnh (`Priority`, `Reason`, `IsInternal`) để các Observer (`NotificationObserver`, `SystemLogObserver`) có đầy đủ dữ liệu mà không cần thực hiện thêm các truy vấn CSDL phụ trợ (tránh N+1 query overhead).
   - Chuyển `User` kế thừa `AggregateRoot<int>` hoặc định nghĩa interface `IHasDomainEvents` để `User` phát sinh `UserDeactivatedEvent`, phục vụ ghi vết kiểm toán của `SystemLogObserver` khi Admin vô hiệu hóa tài khoản khác (TC-10).

2. **Từ Observation 2 (Không có package MediatR) $\to$ Quyết định thiết kế In-Memory Dispatcher**:
   - Để tuân thủ nguyên tắc "Boring Technology" (#050) và giữ `ItTms.Application` gọn nhẹ, thiết kế `IDomainEventObserver<TEvent>` cùng `InMemoryDomainEventDispatcher` dựa trên ASP.NET Core `IServiceProvider`.
   - Giao diện được thiết kế đẳng cấu (isomorphic) với MediatR `INotificationHandler<TNotification>`, cho phép nhóm dự án có thể chuyển sang MediatR chỉ bằng một dòng cấu hình DI nếu muốn.

3. **Từ Observation 1, 2, 3 $\to$ Quyết định thiết kế Quy trình Điều phối Hai Pha (Two-Phase Dispatch)**:
   - *Pha 1 (In-Transaction)*: Trong `ItTmsDbContext.SaveChangesAsync`, ChangeTracker quét tất cả thực thể có Domain Events, gọi `DispatchInTransactionAsync`. `SystemLogObserver` và `NotificationObserver` thêm các thực thể `SystemLog` và `Notification` vào cùng ChangeTracker. Lệnh `base.SaveChangesAsync()` lưu toàn bộ xuống SQLite trong cùng một giao dịch ACID.
   - *Pha 2 (Post-Commit)*: Chỉ sau khi `base.SaveChangesAsync()` thực hiện thành công, `DispatchPostCommitAsync` mới gọi `INotificationDispatcher` phát sóng SignalR WebSocket.
   - *Suy diễn đối kháng (#Gamma-Prime)*: Nếu DB commit thất bại (xung đột `DbUpdateConcurrencyException`), Pha 2 không được gọi. Do đó, hiện tượng thông báo ma ("Ghost Notifications") trên trình duyệt client bị triệt tiêu 100%.

4. **Từ Observation 3 (Bảo vệ thông tin nội bộ trong TC-08)**:
   - Khi có `TicketCommentAddedEvent`, `NotificationObserver` bắt buộc phải kiểm tra cờ `!domainEvent.IsInternal` trước khi gửi thông báo tới User. Nếu là ghi chú nội bộ, thông báo tuyệt đối không được phát tới tài khoản User.

---

## 3. CAVEATS

1. **Phạm vi thẩm tra (Scope Bound)**: Nhiệm vụ của `explorer_m2_2` là thiết kế kiến trúc (Read-only Investigation) và lập blueprint chi tiết. Không trực tiếp chỉnh sửa mã nguồn trong thư mục `src/`.
2. **SignalR Persistent Connection Assumptions**: Giả định rằng `TicketHub` (tại `/hubs/ticket`) đã quản lý các group theo định dạng `ticket_{id}` và `role_{roleName}` như đã được đặc tả trong `PROJECT.md` và `spec_inventory.md`.
3. **Email / SMS Notifications**: Thiết kế hiện tại tập trung vào in-app notifications (`Notifications` table) và SignalR real-time websocket badges. Nếu tương lai cần tích hợp Email/SMS, chỉ cần bổ sung `EmailNotificationObserver` vào DI mà không làm thay đổi Domain Model (Open/Closed Principle).

---

## 4. CONCLUSION

Bản kế hoạch `observer_pattern_plan.md` đã hoàn thành toàn diện, bao quát 100% yêu cầu kỹ thuật:
1. **6 Domain Events**: `TicketCreatedEvent`, `TicketAssignedEvent`, `TicketStatusChangedEvent`, `TicketCommentAddedEvent`, `TicketClosedEvent`, `UserDeactivatedEvent`.
2. **2 Concrete Observers**: `NotificationObserver` và `SystemLogObserver` đóng gói toàn bộ logic phản ứng cho các kịch bản TC-03, TC-04, TC-08, TC-09, TC-10.
3. **Cơ chế Two-Phase In-Memory Event Dispatcher**: Đảm bảo toàn vẹn dữ liệu ACID trên SQLite, chống lặp sự kiện vô hạn, chống thông báo ảo khi rollback transaction, và tích hợp mượt mà với ASP.NET Core SignalR.

---

## 5. VERIFICATION METHOD

Các kiểm chứng độc lập mà lập trình viên và auditor có thể thực hiện:

1. **Kiểm tra sự tồn tại và tính nhất quán của bản vẽ thiết kế**:
   - Mở file: `c:\IT_System\.agents\teamwork\explorer_m2_2\observer_pattern_plan.md`.
   - Xác minh mã nguồn mẫu, bảng phân loại sự kiện, cấu hình DI và luồng thực thi 2 pha.

2. **Kiểm tra tính tương thích biên dịch**:
   - Chạy lệnh kiểm tra build hiện tại của dự án:
     ```powershell
     dotnet build c:\IT_System\ItTms.sln
     ```
   - Kết quả mong đợi: `Build succeeded. 0 Warning(s). 0 Error(s).`

3. **Kịch bản kiểm thử đơn vị khi triển khai (Unit Test Verification)**:
   - Kiểm tra `NotificationObserver`:
     - Giả lập `TicketCreatedEvent` $\to$ Assert số lượng bản ghi `Notification` bằng số lượng Leader có `Status == Active`.
     - Giả lập `TicketCommentAddedEvent` với `IsInternal = true` $\to$ Assert không có Notification nào gửi tới User tạo ticket.
   - Kiểm tra `SystemLogObserver`:
     - Giả lập `TicketStatusChangedEvent` $\to$ Assert bản ghi `SystemLog` được tạo với `Action = "UpdateStatus"`.
   - Kiểm tra `InMemoryDomainEventDispatcher`:
     - Khi `base.SaveChangesAsync()` ném ngoại lệ $\to$ Assert `INotificationDispatcher.DispatchDomainEventAsync` không được gọi (zero invocation).
