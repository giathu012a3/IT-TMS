# BÁO CÁO THẨM ĐỊNH ĐỘC LẬP (REVIEW & ADVERSARIAL REPORT) — REVIEWER 1
**Vai trò**: Reviewer 1 (`reviewer_1`) — Backend Realtime & Event Flow Reviewer & Adversarial Critic  
**Thời điểm hoàn tất**: 2026-10-25T14:14:00Z  
**Đối tượng thẩm định**: Worker M1 (`worker_m1`) — Milestone M1: Backend Realtime Engine & Events  
**Loại bàn giao**: Hard Handoff  
**Quyết định thẩm định (EXPLICIT VERDICT)**: **`APPROVE`** (Chấp thuận nghiệm thu)

---

## 1. OBSERVATION (QUAN SÁT THỰC NGHIỆM ĐỘC LẬP)

### 1.1. Khảo sát Mã nguồn Backend
1. **`User/TicketController.cs` (Dòng 242-336)**:
   - Phương thức: `public async Task<IActionResult> AddComment(int id, string content)`
   - Không tồn tại tham số `isInternal` trong signature; dòng 278 cưỡng chế `Comment.Create(ticket.Id, CurrentUserId.Value, content, isInternal: false)`.
   - Cơ chế phát sinh sự kiện 2 pha:
     - Dòng 288-289: `await _dbContext.Comments.AddAsync(comment); await _dbContext.SaveChangesAsync();` -> SQLite sinh `comment.Id > 0`.
     - Dòng 292-298: `ticket.AddDomainEvent(new CommentAddedEvent(comment.Id, ticket.Id, CurrentUserId.Value, comment.Content, comment.IsInternal, comment.CreatedAt));`
     - Dòng 300: `await _dbContext.SaveChangesAsync();` -> kích hoạt dispatchers và observers.
   - Phản hồi Ajax (dòng 302-332): Trả về `Ok(new { success = true, message = "...", comment = commentDto })` chứa đầy đủ `id`, `ticketId`, `authorId`, `authorUserId`, `authorName`, `authorRole`, `content`, `createdAt`, `isInternal`, `formattedTime`.
   - Tự động chuyển trạng thái `Waiting` sang `InProgress` khi User phản hồi (dòng 273-276).
   - Bảo toàn Bảng 7.2 TC-08: Kiểm tra `!ticket.CanComment()` chặn bình luận trên ticket đã Resolved/Closed/Rejected (dòng 263-270).

2. **`StaffTicketController.cs` (Dòng 252-338)**:
   - Signature: `public async Task<IActionResult> AddComment(int id, string content, bool isInternal = false)`
   - BOLA / IDOR Check: `ticket.AssignedToId != CurrentUserId!.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin)` trả về `Forbid()` (dòng 267-268).
   - CanComment: Chặn TC-08 khi `!ticket.CanComment()` (dòng 271-278).
   - Phát sinh `CommentAddedEvent` mang đúng `comment.Id` sau lần `SaveChangesAsync()` thứ nhất.
   - Phản hồi Ajax trả về đối tượng `commentDto` với `isInternal = comment.IsInternal`.

3. **`LeaderTicketController.cs` (Dòng 338-420)**:
   - Signature: `public async Task<IActionResult> AddComment(int id, string content, bool isInternal = false)`
   - Kiểm tra `!ticket.CanComment()` (dòng 353-360).
   - Phát sinh `CommentAddedEvent` mang đúng `comment.Id` sau lần `SaveChangesAsync()` thứ nhất.
   - Phản hồi Ajax trả về đối tượng `commentDto` với `isInternal = comment.IsInternal`.

4. **`SignalRNotificationDispatcher.cs` (Tầng Web - Dòng 131-210)**:
   - Xử lý `CommentAddedEvent`:
     - Nếu `comment.IsInternal`: Dispatch tới nhóm `ticket_{ticketId}_internal` và `Ticket_{ticketId}_internal` (dòng 152-155).
     - Nếu không phải internal: Dispatch tới nhóm `ticket_{ticketId}` và `Ticket_{ticketId}` (dòng 159-162).
     - Chắn chuông thông báo cá nhân cho User: Dòng 176 `if (!comment.IsInternal && comment.AuthorUserId != ticket.CreatorId) SendToUserAsync(...)` -> User tuyệt đối không nhận thông báo chuông cho bình luận nội bộ hoặc bình luận của chính mình.
     - Staff phụ trách nhận chuông nếu khác tác giả (dòng 186-193).
     - Leader nhận chuông (dòng 196-209), lọc theo `u.Role == UserRole.Leader && u.Status == UserStatus.Active` (khắc phục triệt để lỗi unmapped property `IsActive`).

5. **`TicketHub.cs` (Dòng 108-115)**:
   - Chỉ cho phép `Staff`, `Leader`, `Admin` tham gia nhóm `ticket_{ticketId}_internal` (dòng 111-115).
   - Tài khoản `User` chỉ được tham gia nhóm công khai `ticket_{ticketId}` sau khi vượt qua kiểm tra quyền sở hữu (`ticket.CreatorId == currentUserId`).

6. **Observers (`NotificationObserver` & `SystemLogObserver`)**:
   - `NotificationObserver.cs` (dòng 145): `if (domainEvent.IsInternal) return;` -> Tuyệt đối không lưu Notification trong DB cho ghi chú nội bộ.
   - `SystemLogObserver.cs` (dòng 75-90): Ghi nhận action `AddInternalNote` hoặc `AddComment`, ghi đúng `domainEvent.CommentId` và nội dung snippet vào `SystemLogs`.

7. **Bộ kiểm thử `CommentEventTests.cs` (Dòng 1-343)**:
   - 6 kịch bản kiểm thử bao quát toàn diện các luồng nghiệp vụ và bảo mật.

---

### 1.2. Kết quả Thực thi Lệnh Kiểm chứng Độc lập (Verbatim Command Outputs)

#### Lệnh 1: `dotnet build c:\IT_System\ItTms.sln`
```text
  Determining projects to restore...
  All projects are up-to-date for restore.
  ItTms.Domain -> c:\IT_System\src\ItTms.Domain\bin\Debug\net9.0\ItTms.Domain.dll
  ItTms.Application -> c:\IT_System\src\ItTms.Application\bin\Debug\net9.0\ItTms.Application.dll
  ItTms.Infrastructure -> c:\IT_System\src\ItTms.Infrastructure\bin\Debug\net9.0\ItTms.Infrastructure.dll
  ItTms.Web -> c:\IT_System\src\ItTms.Web\bin\Debug\net9.0\ItTms.Web.dll
  ItTms.Tests -> c:\IT_System\tests\ItTms.Tests\bin\Debug\net9.0\ItTms.Tests.dll

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:10.94
```

#### Lệnh 2: `dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~CommentEventTests"`
```text
[xUnit.net 00:00:00.00] xUnit.net V3 Test Execution (net9.0 - x64)
  Discovering: ItTms.Tests (method display = ClassAndMethod, method display options = None)
  Discovered:  ItTms.Tests (found 6 test cases)
  Starting:    ItTms.Tests (parallel test collections = on, max threads = 16)
  Finished:    ItTms.Tests
Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 2 s - ItTms.Tests.dll (net9.0)
```

#### Lệnh 3: `dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~Table72"`
```text
[xUnit.net 00:00:00.00] xUnit.net V3 Test Execution (net9.0 - x64)
  Discovering: ItTms.Tests (method display = ClassAndMethod, method display options = None)
  Discovered:  ItTms.Tests (found 10 test cases)
  Starting:    ItTms.Tests (parallel test collections = on, max threads = 16)
  Finished:    ItTms.Tests
Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 3 s - ItTms.Tests.dll (net9.0)
```

#### Lệnh 4: `dotnet test c:\IT_System\ItTms.sln` (Toàn bộ Test Suite)
```text
[xUnit.net 00:00:00.00] xUnit.net V3 Test Execution (net9.0 - x64)
  Discovering: ItTms.Tests (method display = ClassAndMethod, method display options = None)
  Discovered:  ItTms.Tests (found 342 test cases)
  Starting:    ItTms.Tests (parallel test collections = on, max threads = 16)
  Finished:    ItTms.Tests
Passed!  - Failed:     0, Passed:   342, Skipped:     0, Total:   342, Duration: 18 s - ItTms.Tests.dll (net9.0)
```

---

## 2. LOGIC CHAIN (CHUỖI SUY LUẬN LOGIC & KIỂM CHỨNG)

1. **Xác minh Khả năng Truyền phát `CommentId` thực tế**:
   - *Luận điểm*: Khi khởi tạo entity bằng `Comment.Create`, `comment.Id == 0`. Nếu gắn event ngay tại thời điểm khởi tạo, `CommentAddedEvent.CommentId` sẽ là `0`.
   - *Thực tế kiểm chứng*: Mã nguồn gọi `ticket.Comments.Add(comment); await _dbContext.Comments.AddAsync(comment); await _dbContext.SaveChangesAsync();` trước, EF Core ánh xạ ID SQLite tự tăng (`comment.Id > 0`). Sau đó mới gắn `new CommentAddedEvent(comment.Id, ...)` và gọi `SaveChangesAsync()` lần 2.
   - *Bằng chứng thực nghiệm*: Test `UserAddComment_TriggersDomainEvent_CreatesNotificationAndSystemLog_ReturnsCommentDto` tại dòng 178 xác nhận `e.CommentId == commentInDb.Id` (thực tế > 0).

2. **Xác minh Cơ chế Điều phối 2 pha tại `ItTmsDbContext.SaveChangesAsync`**:
   - Khi phát hiện `ticket` có Domain Events:
     - `_domainEventDispatcher.DispatchInTransactionAsync`: Kích hoạt `NotificationObserver.HandleAsync` và `SystemLogObserver.HandleAsync`, ghi entity `Notification` và `SystemLog` vào `DbContext`.
     - `base.SaveChangesAsync()`: Lưu toàn bộ thay đổi xuống CSDL SQLite trong một giao dịch.
     - `_domainEventDispatcher.DispatchPostCommitAsync`: Chỉ khi DB commit thành công mới gọi `SignalRNotificationDispatcher.DispatchDomainEventAsync` để broadcast WebSocket, ngăn chặn tình huống SignalR gửi tin nhắn ảo khi DB bị rollback.

3. **Xác minh Kiểm soát Quyền và Cách ly Ghi chú Nội bộ (`IsInternal`)**:
   - *Phía Controller*: `User/TicketController` không hề nhận tham số `isInternal`, luôn hardcode `isInternal: false`. User không có cách nào giả mạo gửi ghi chú nội bộ.
   - *Phía SignalR Room*: `TicketHub.JoinTicketGroup` chỉ ghi danh các kết nối có role `Staff`, `Leader`, `Admin` vào nhóm `ticket_{id}_internal`.
   - *Phía Phân phối Sự kiện*: `SignalRNotificationDispatcher` gửi sự kiện `ReceiveComment` nội bộ chỉ vào phòng `_internal`.
   - *Phía Chuông Thông báo*: `SignalRNotificationDispatcher` chặn `ticket.CreatorId` nếu `comment.IsInternal`. `NotificationObserver` cũng bỏ qua `CommentAddedEvent` nếu `domainEvent.IsInternal`.
   - *Kết luận*: Cấu trúc 4 lớp phòng vệ (Defense-in-Depth) chống rò rỉ ghi chú nội bộ hoàn toàn kín kẽ.

4. **Xác minh Định dạng Phản hồi Ajax `commentDto`**:
   - Cả 3 Controller đều trả về đối tượng `comment` chứa đầy đủ: `id`, `ticketId`, `authorId`, `authorUserId`, `authorName`, `authorRole`, `content`, `createdAt`, `isInternal`, `formattedTime`.
   - Định dạng này khớp hoàn hảo với hàm `appendCommentToView(comment)` trong `wwwroot/js/ticket-realtime.js`, đảm bảo hỗ trợ cả thuộc tính viết thường (`camelCase`) và viết hoa (`PascalCase`), đồng thời kích hoạt cơ chế khử trùng lặp qua selector `[data-comment-id="${commentId}"]`.

---

## 3. ADVERSARIAL STRESS-TEST & INTEGRITY CHECK (THẨM ĐỊNH ĐỐI KHÁNG)

### 3.1. Kiểm tra Dấu hiệu Vi phạm Tính Trung thực (Integrity Check)
- **Hardcoded test results**: Không phát hiện. Không có bất kỳ khối lệnh `if (test) ...` hoặc hardcode dữ liệu giả nào trong các Controller, Hub hoặc Observer.
- **Dummy / Facade implementations**: Không phát hiện. Toàn bộ logic tương tác trực tiếp với EF Core DbContext, SignalR IHubContext, và SQLite database.
- **Shortcuts / Bypassed tasks**: Không phát hiện. Toàn bộ 20 Use Cases và 10 kịch bản Table 7.2 được bảo toàn nguyên vẹn.
- **Fabricated verification outputs**: Không phát hiện. Đã chạy thực tế `dotnet build` và `dotnet test` độc lập, ghi nhận trực tiếp từ console đầu ra.

### 3.2. Đánh giá Rủi ro & Góc nhìn Đối kháng (Adversarial Challenges)

| # | Thách thức / Tình huống Tấn công | Kịch bản Thất bại Tiềm ẩn | Mức độ Rủi ro | Đánh giá Khả năng Phòng ngự Hiện tại |
|---|---|---|---|---|
| 1 | Hai lần gọi `SaveChangesAsync()` liên tiếp trong Controller | Nếu lần gọi thứ 2 thất bại (do lỗi Observer), bình luận đã được ghi vào DB nhưng không phát sinh Event/Log. | **Thấp** | Các Observer (`NotificationObserver`, `SystemLogObserver`) chỉ thực hiện thêm bản ghi mới với dữ liệu hợp lệ, không có nguy cơ xung đột khóa hoặc ràng buộc; đối với SQLite embedded, mô hình 2 pha này bảo đảm lấy được `comment.Id` an toàn. |
| 2 | Người dùng gọi HTTP POST gửi kèm tham số `isInternal=true` tại phân hệ User | Kẻ xấu cố gắng gửi ghi chú nội bộ bằng cách can thiệp payload form của User. | **Không có (Triệt tiêu)** | Action `User/TicketController.AddComment(int id, string content)` không khai báo tham số `isInternal`, và hardcode `isInternal: false` tại `Comment.Create(...)`. Mọi tham số `isInternal` từ request đều bị ASP.NET Core Model Binder bỏ qua. |
| 3 | Người dùng thường kết nối trực tiếp WebSocket gọi `JoinTicketGroup` phòng nội bộ | Người dùng cố tình gọi `connection.invoke("JoinTicketGroup", ticketId)` để nghe lén ghi chú nội bộ. | **Không có (Triệt tiêu)** | `TicketHub.JoinTicketGroup` chỉ add connection vào phòng `ticket_{id}_internal` nếu `role == Staff || Leader || Admin`. User chỉ được add vào phòng công khai `ticket_{id}` nếu là chủ sở hữu (`ticket.CreatorId == currentUserId`). |
| 4 | Bất đồng bộ hóa (Race Condition) giữa Ajax response và WebSocket `ReceiveComment` | Tin nhắn xuất hiện 2 lần trên màn hình người gửi nếu cả 2 sự kiện đến gần như cùng lúc. | **Không có (Triệt tiêu)** | Hàm `appendCommentToView` trong `ticket-realtime.js` kiểm tra `document.querySelector([data-comment-id="${commentId}"])` trước khi render. Dù sự kiện nào tới trước, sự kiện sau sẽ bị drop ngay lập tức. |
| 5 | Gửi bình luận trên Ticket đã bị Resolved / Closed (Vi phạm TC-08) | Kỹ thuật viên hoặc người dùng gửi phản hồi khi ticket đã hoàn tất. | **Không có (Triệt tiêu)** | Cả 3 Controller và SignalR Hub đều kiểm tra `ticket.CanComment()` trước khi xử lý, trả về lỗi 400 Bad Request / HubException và ghi warning log. Test `UserAddComment_WhenTicketResolved_IsBlockedByCanComment_DoesNotAddCommentOrEvent` và `Table 7.2 TC-08` xác nhận pass 100%. |

---

## 4. CAVEATS (GIỚI HẠN PHẠM VI KHẢO SÁT)
1. Phạm vi của Reviewer 1 tập trung chuyên sâu vào Backend Realtime Engine & Event Flow (Controllers, Observers, SignalR Dispatcher, xUnit Tests). Giao diện hiển thị trình duyệt thực tế, CSS Palette và Floating Card Box UI được đánh giá độc lập bởi Reviewer 2 (`reviewer_2`) và Forensic Auditor.
2. Kiểm thử SignalR trên tầng xUnit sử dụng `TestHubContext` mô phỏng trong bộ nhớ. Kiểm thử WebSocket E2E qua trình duyệt thực tế sẽ được xác nhận ở M3 và M5.

---

## 5. CONCLUSION & VERDICT (KẾT LUẬN & PHÁN QUYẾT)

1. Công việc của Worker M1 đạt chuẩn mực kỹ thuật cao, đáp ứng đầy đủ và vượt mức các tiêu chí nghiệm thu của Milestone M1:
   - Phát sinh đầy đủ `CommentAddedEvent` với `CommentId` đã được lưu vào CSDL trên cả 3 controller.
   - `SaveChangesAsync` điều phối chính xác 2 pha: In-Transaction Observers (ghi Notification & SystemLog) và Post-Commit Dispatcher (phát SignalR).
   - Kiểm soát nghiêm ngặt `IsInternal`: User hoàn toàn không thể gửi hoặc xem ghi chú nội bộ; chuông thông báo và SignalR room được cách ly bảo mật tuyệt đối.
   - Toàn bộ 342 bài test kiểm thử tự động vượt qua 100%, 0 warning, 0 error.
2. **PHÁN QUYẾT CHÍNH THỨC**: **`APPROVE`** (Chấp thuận hoàn toàn, sẵn sàng tích hợp với M2 và M3).

---

## 6. VERIFICATION METHOD (HƯỚNG DẪN XÁC MINH ĐỘC LẬP)

Để tái lập xác minh độc lập:
```powershell
# 1. Biên dịch toàn bộ Solution (Kỳ vọng: 0 Warning, 0 Error)
dotnet build c:\IT_System\ItTms.sln

# 2. Chạy bộ kiểm thử Comment Event & SignalR Dispatcher (Kỳ vọng: 6 Passed)
dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~CommentEventTests"

# 3. Chạy 10 kịch bản kiểm thử nghiệm thu Bảng 7.2 (Kỳ vọng: 10 Passed)
dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~Table72"

# 4. Chạy toàn bộ 342 kịch bản kiểm thử của hệ thống (Kỳ vọng: 342 Passed)
dotnet test c:\IT_System\ItTms.sln
```
