# BÁO CÁO BÀN GIAO NHIỆM VỤ (HANDOFF REPORT) — WORKER M1
**Milestone**: M1 - Backend Realtime Engine & Events  
**Tác giả**: Worker M1 (`worker_m1`) — Roles: Implementer, QA, Specialist  
**Thời điểm bàn giao**: 2026-10-25T14:08:00Z  
**Loại bàn giao**: Hard Handoff (Hoàn tất 100% nhiệm vụ)

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM)

1. **Khuyết thiếu phát sinh Domain Event tại 3 Controller**:
   - Trước sửa đổi, `User/TicketController.AddComment` (dòng 285-289), `StaffTicketController.AddComment` (dòng 287-291) và `LeaderTicketController.AddComment` (dòng 369-373) chỉ gọi `_dbContext.Comments.AddAsync(...)` rồi `SaveChangesAsync()`.
   - Đối tượng `ticket` không hề được gọi `ticket.AddDomainEvent(new CommentAddedEvent(...))`.
   - Kết quả: `ItTmsDbContext.SaveChangesAsync` trong `ChangeTracker.Entries<IHasDomainEvents>()` không tìm thấy sự kiện nào (`entitiesWithEvents.Count == 0`), dẫn tới `_domainEventDispatcher.DispatchInTransactionAsync` (kích hoạt `NotificationObserver`, `SystemLogObserver`) và `_domainEventDispatcher.DispatchPostCommitAsync` (kích hoạt `SignalRNotificationDispatcher.HandleCommentAddedAsync`) hoàn toàn bị bỏ qua khi gửi bình luận qua HTTP POST!
2. **Khuyết thiếu dữ liệu phản hồi Ajax**:
   - Cả 3 Controller chỉ trả về JSON thô `{ success = true, message = "..." }` mà không trả về đối tượng `CommentDto` (thiếu `id`, `ticketId`, `authorId`, `authorName`, `authorRole`, `content`, `createdAt`, `isInternal`).
3. **Lỗ hổng phân phối thông báo chuông SignalR**:
   - Trong `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs`, hàm `HandleCommentAddedAsync` chỉ bắn sự kiện `ReceiveComment` vào group `ticket_{id}`, mà hoàn toàn chưa dispatch thông báo chuông cá nhân `ReceiveNotification` và `UpdateUnreadNotificationCount` tới các bên đối tác (counterpart users).
   - Truy vấn LINQ trước đây dùng `u.IsActive` là thuộc tính unmapped trên thực thể `User`, gây lỗi `InvalidOperationException: Translation of member 'IsActive' on entity type 'User' failed` khi EF Core chuyển dịch sang SQL SQLite.
4. **Kết quả kiểm thử trước và sau**:
   - Trước: 336 tests pass, nhưng 0 bài test nào kiểm thử luồng tích hợp từ `AddComment` -> phát sinh `CommentAddedEvent` -> sinh `Notification` / `SystemLog` -> dispatch SignalR.
   - Sau: Bổ sung `tests/ItTms.Tests/UnitTests/CommentEventTests.cs` gồm 6 kịch bản kiểm thử độc lập; nâng tổng số bài test lên **342 tests**, toàn bộ **342/342 tests PASSED 100%**, thời gian chạy 20 giây.

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN LOGIC)

1. **Từ Quan sát 1 -> Giải pháp Phát sinh Domain Event 2 pha (Two-Phase Commit)**:
   - Trong SQLite với AutoIncrement ID, khi một `Comment` mới được khởi tạo bằng `Comment.Create(...)`, thuộc tính `comment.Id` có giá trị `0`.
   - Để `CommentAddedEvent` mang đúng `CommentId` thực tế phục vụ logging và SignalR payload, quy trình được chia thành 2 pha nguyên tử:
     - **Pha 1**: `ticket.Comments.Add(comment); await _dbContext.Comments.AddAsync(comment); await _dbContext.SaveChangesAsync();` -> SQLite cấp phát `comment.Id > 0`.
     - **Pha 2**: Khởi tạo `CommentAddedEvent(comment.Id, ticket.Id, CurrentUserId.Value, comment.Content, comment.IsInternal, comment.CreatedAt)` và gắn vào `ticket.AddDomainEvent(...)`, sau đó gọi `await _dbContext.SaveChangesAsync();`.
     - Tại Pha 2, `ItTmsDbContext` phát hiện `ticket` có Domain Event, lập tức kích hoạt `_domainEventDispatcher.DispatchInTransactionAsync` (lưu `Notification` và `SystemLog` trong cùng Transaction của DbContext), cam kết DB và kích hoạt `_domainEventDispatcher.DispatchPostCommitAsync` (bắn SignalR WebSocket).
2. **Từ Quan sát 2 -> Chuẩn hóa Phản hồi Ajax DTO**:
   - Khi `IsAjaxRequest` là true, Controller truy vấn thông tin tác giả từ `_dbContext.Users` bằng LINQ tối ưu `.Where(u => u.Id == CurrentUserId.Value).Select(u => new { u.FullName, u.Role }).FirstOrDefaultAsync()`.
   - Trả về payload JSON:
     ```json
     {
       "success": true,
       "message": "...",
       "comment": {
         "id": 123,
         "ticketId": 45,
         "authorId": 10,
         "authorUserId": 10,
         "authorName": "Alice Creator",
         "authorRole": "User",
         "content": "...",
         "createdAt": "2026-10-25T...",
         "isInternal": false,
         "formattedTime": "Vừa xong"
       }
     }
     ```
   - Cung cấp đồng thời cả `authorId` lẫn `authorUserId` để tương thích hoàn toàn với cả mã frontend `ticket-realtime.js` và đặc tả hợp đồng `PROJECT.md`.
3. **Từ Quan sát 3 -> Định tuyến Thông báo Chuông & Chắn Ghi chú Nội bộ**:
   - Tại `SignalRNotificationDispatcher.HandleCommentAddedAsync`:
     - Bình luận công khai (`IsInternal == false`):
       - Bắn `ReceiveComment` tới phòng `ticket_{ticketId}` và `Ticket_{ticketId}`.
       - Nếu tác giả là User: Gửi chuông `ReceiveNotification` tới Staff phụ trách (nếu có) và tất cả Leaders đang hoạt động (`Status == UserStatus.Active`).
       - Nếu tác giả là Staff: Gửi chuông tới User tạo ticket và các Leaders.
       - Nếu tác giả là Leader: Gửi chuông tới User tạo ticket và Staff phụ trách.
     - Bình luận nội bộ (`IsInternal == true`):
       - Bắn `ReceiveComment` tới phòng nội bộ `ticket_{ticketId}_internal` và `Ticket_{ticketId}_internal`.
       - Chắn tuyệt đối User: `if (!comment.IsInternal && comment.AuthorUserId != ticket.CreatorId) SendToUserAsync(...)` đảm bảo User không bao giờ nhận được chuông thông báo.
       - Chỉ gửi chuông tới Staff phụ trách và các Leaders với thông điệp ghi rõ "ghi chú nội bộ".
     - Chữa lỗi dịch LINQ: Thay `u.IsActive` bằng `u.Status == UserStatus.Active` để EF Core biên dịch thành SQL `WHERE Status = 1`.
4. **Từ Quan sát 4 -> Bộ Kiểm thử Toàn diện `CommentEventTests.cs`**:
   - Thiết kế 6 kịch bản kiểm thử:
     - `UserAddComment_TriggersDomainEvent_CreatesNotificationAndSystemLog_ReturnsCommentDto`
     - `StaffAddComment_InternalNote_AddsDomainEvent_ShieldsUserFromNotification_LogsInternalNote`
     - `LeaderAddComment_Public_TriggersDomainEvent_NotifiesUserAndStaff`
     - `SignalRNotificationDispatcher_HandleCommentAddedAsync_PublicComment_DispatchesReceiveComment_AndNotifiesCounterparts`
     - `SignalRNotificationDispatcher_HandleCommentAddedAsync_InternalComment_StrictlyShieldsUser_OnlyDispatchesInternalAndStaffLeader`
     - `UserAddComment_WhenTicketResolved_IsBlockedByCanComment_DoesNotAddCommentOrEvent` (Bảo toàn Bảng 7.2 TC-08).

---

## 3. CAVEATS (LƯU Ý VÀ GIẢ ĐỊNH)

1. **Phân tách trách nhiệm kiểm thử**: Bộ kiểm thử `CommentEventTests.cs` sử dụng `_mockNotificationDispatcher` cho các bài kiểm thử Controller nhằm cô lập kiểm thử tầng HTTP/Domain/Observers và ngăn ngừa xung đột SQLite connection locking khi nhiều DbContext cùng ghi trên `:memory:`. Tầng SignalR dispatcher được kiểm thử chuyên sâu trực tiếp với `TestHubContext`.
2. **Quyền riêng tư của User**: User Controller hoàn toàn không nhận tham số `isInternal` từ client, luôn cưỡng chế `isInternal: false`. Tại SignalR Dispatcher và Observer, mọi logic đều có chốt chặn fail-closed đối với `IsInternal`.

---

## 4. CONCLUSION (KẾT LUẬN)

1. Milestone M1 (Backend Realtime Engine & Events) đã được hoàn thiện đạt chuẩn 100% yêu cầu:
   - 3 Controller (`User/TicketController`, `StaffTicketController`, `LeaderTicketController`) đã phát sinh đầy đủ `CommentAddedEvent` và trả về `CommentDto` hoàn chỉnh cho Ajax.
   - `SignalRNotificationDispatcher.HandleCommentAddedAsync` đã định tuyến chính xác sự kiện phòng và chuông thông báo cá nhân, bảo vệ tuyệt đối bình luận nội bộ trước người dùng thường.
   - Bộ giải pháp biên dịch với 0 lỗi, 0 cảnh báo dưới `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.
   - Toàn bộ 342 bài test (gồm 336 bài test cơ sở + 6 bài test mới) vượt qua 100%.

---

## 5. VERIFICATION METHOD (PHƯƠNG PHÁP XÁC MINH ĐỘC LẬP)

Để kiểm chứng độc lập kết quả bàn giao, Forensic Auditor hoặc Orchestrator thực hiện các lệnh sau:

### 1. Kiểm tra Biên dịch
```powershell
dotnet build c:\IT_System\ItTms.sln
```
*Kỳ vọng*: `Build succeeded. 0 Warning(s), 0 Error(s)`.

### 2. Kiểm thử Riêng Bộ Kiểm thử Comment & SignalR
```powershell
dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~CommentEventTests"
```
*Kỳ vọng*: `Passed! - Failed: 0, Passed: 6, Skipped: 0, Total: 6`.

### 3. Kiểm thử Toàn bộ Solution
```powershell
dotnet test c:\IT_System\ItTms.sln
```
*Kỳ vọng*: `Passed! - Failed: 0, Passed: 342, Skipped: 0, Total: 342`.

### 4. Kiểm tra Tệp Mã Nguồn Đã Chỉnh Sửa
- `src/ItTms.Web/Areas/User/Controllers/TicketController.cs` (Dòng 285 - 335)
- `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs` (Dòng 285 - 335)
- `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs` (Dòng 365 - 415)
- `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs` (Dòng 130 - 215)
- `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs` (Dòng 35 - 55)
- `tests/ItTms.Tests/UnitTests/CommentEventTests.cs` (Toàn bộ tệp)
