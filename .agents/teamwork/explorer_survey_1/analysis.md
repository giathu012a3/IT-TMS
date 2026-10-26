# BÁO CÁO KHẢO SÁT CHUYÊN SÂU KIẾN TRÚC BACKEND REAL-TIME SIGNALR, DOMAIN EVENTS VÀ COMMENT HANDLING

> **Dự án**: IT Ticket Management System (IT-TMS) .NET 9 Clean Architecture  
> **Người thực hiện**: Backend Realtime Explorer (`survey_explorer_1`)  
> **Thời điểm hoàn thành**: 2026-10-25T13:30:00Z  
> **Mục tiêu**: Khảo sát toàn diện kiến trúc SignalR, luồng xử lý comment, domain events, phân tách bình luận nội bộ (`IsInternal`), đánh giá hiện trạng kiểm thử và đề xuất bản thiết kế kiến trúc (Blueprint) chi tiết cho đội ngũ triển khai.

---

## TỔNG QUAN PHÁT HIỆN CỐT LÕI (EXECUTIVE SUMMARY)

1. **Nguyên nhân gốc rễ lỗi không nhận bình luận qua SignalR**: Cả 3 Controller (`User/TicketController:241`, `Staff/StaffTicketController:251`, `Leader/LeaderTicketController:337`) lưu trực tiếp `Comment` vào `_dbContext.Comments` mà **hoàn toàn không phát Domain Event `CommentAddedEvent`** gắn vào Ticket hay gọi `_domainEventDispatcher`. Hậu quả là `ItTmsDbContext.SaveChangesAsync` không phát hiện sự kiện nào trong ChangeTracker, dẫn tới `NotificationObserver`, `SystemLogObserver` và `SignalRNotificationDispatcher.HandleCommentAddedAsync` **bị bỏ qua 100% khi gửi qua HTTP POST**.
2. **Khuyết thiếu phản hồi Ajax**: Khi nhận yêu cầu Ajax (`X-Requested-With: XMLHttpRequest`), cả 3 Controller chỉ trả về JSON thô `{ success = true, message = "..." }` mà **không trả về đối tượng `CommentDto`** (thiếu `id`, `authorName`, `createdAt`, `isInternal`...). Phía client `ticket-realtime.js` sau khi gửi chỉ xóa ô nhập liệu và thụ động chờ SignalR (vốn không bao giờ được kích hoạt), dẫn tới hiện tượng "gửi xong biến mất".
3. **Phân tách bình luận nội bộ (`IsInternal = true`) có thiết kế chuẩn nhưng cần kích hoạt**: Cơ chế phân quyền BOLA và bảo mật 4 lớp (`Query Filter`, `SignalR Group Segmentation`, `Observer Guard`, `Input Sanitization`) đã được thiết kế sẵn sàng: Client `User` không bao giờ được gia nhập phòng `ticket_{id}_internal` và `NotificationObserver` đã có lệnh chặn `if (domainEvent.IsInternal) return;`. Tuy nhiên, vì Domain Event không được bắn nên toàn bộ luồng bảo vệ này chưa từng được vận hành trong thực tế sử dụng.
4. **Khoảng trống kiểm thử (Test Gaps)**: Bộ kiểm thử hiện tại có 336 bài test pass 100% nhưng chỉ kiểm thử riêng rẽ ở tầng Unit/Mock (`SignalRNotificationDispatcherTests` tự tạo `new CommentAddedEvent()`, `ObserverPatternTests` tự gọi `HandleAsync()`, `TicketHubTests` gọi trực tiếp hàm Hub WebSocket). **Tuyệt đối chưa có bất kỳ bài test tích hợp nào kiểm tra luồng từ HTTP POST `AddComment` -> phát sinh `CommentAddedEvent` -> sinh Notification trong DB -> phát SignalR `ReceiveComment`**.

---

## 1. PHÂN TÍCH CHI TIẾT `AddComment` TRÊN 3 CONTROLLERS

### 1.1. `User/TicketController.AddComment`
- **Tệp nguồn**: `src/ItTms.Web/Areas/User/Controllers/TicketController.cs` (Dòng 241 - 293)
- **Chữ ký**: `public async Task<IActionResult> AddComment(int id, string content)`
- **Các bước thực thi hiện tại**:
  1. Kiểm tra rỗng: `string.IsNullOrWhiteSpace(content)` -> nếu Ajax trả về `BadRequest({ message = "..." })`, nếu thường trả về `TempData` và `RedirectToAction`.
  2. Nạp Ticket: `_dbContext.Tickets.Include(t => t.Status).FirstOrDefaultAsync(t => t.Id == id)`.
  3. BOLA Defense: Chặn nếu `ticket.CreatorId != CurrentUserId.Value` và không phải Leader/Admin.
  4. Ràng buộc Bảng 7.2 TC-08: Kiểm tra `!ticket.CanComment()` -> chặn nếu trạng thái là Resolved (5), Closed (6), Rejected (7).
  5. Tự động chuyển trạng thái: `if (ticket.StatusId == TicketStatus.Waiting) ticket.StartProgress(CurrentUserId.Value);`.
  6. Tạo thực thể: `var commentResult = Comment.Create(ticket.Id, CurrentUserId.Value, content, isInternal: false);` (User bị cưỡng chế `isInternal: false` tại mã nguồn).
  7. Lưu CSDL: `await _dbContext.Comments.AddAsync(commentResult.Value); await _dbContext.SaveChangesAsync();`.
  8. Trả về kết quả:
     ```csharp
     if (IsAjaxRequest)
         return Ok(new { success = true, message = "Bình luận đã gửi thành công." });
     TempData["SuccessMessage"] = "Bình luận đã được gửi.";
     return RedirectToAction(nameof(Details), new { id });
     ```

### 1.2. `Staff/StaffTicketController.AddComment`
- **Tệp nguồn**: `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs` (Dòng 251 - 295)
- **Chữ ký**: `public async Task<IActionResult> AddComment(int id, string content, bool isInternal = false)`
- **Các bước thực thi hiện tại**:
  1. Kiểm tra rỗng `content`.
  2. Nạp Ticket.
  3. BOLA Defense: Chặn nếu `ticket.AssignedToId != CurrentUserId.Value` và không phải Leader/Admin.
  4. Bảng 7.2 TC-08: `!ticket.CanComment()` -> chặn.
  5. Tạo thực thể: `Comment.Create(ticket.Id, CurrentUserId.Value, content, isInternal)`.
  6. Lưu CSDL: `_dbContext.Comments.AddAsync` -> `_dbContext.SaveChangesAsync`.
  7. Trả về Ajax: `return Ok(new { success = true, message = "Bình luận đã gửi." });`.

### 1.3. `Leader/LeaderTicketController.AddComment`
- **Tệp nguồn**: `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs` (Dòng 337 - 377)
- **Chữ ký**: `public async Task<IActionResult> AddComment(int id, string content, bool isInternal = false)`
- **Các bước thực thi hiện tại**:
  1. Tương tự Staff, có quyền quản lý toàn cục không bị chặn BOLA.
  2. Kiểm tra `!ticket.CanComment()`.
  3. Tạo Comment -> Lưu CSDL -> Trả về Ajax `{ success = true, message = "Bình luận đã gửi." }`.

### 1.4. Chẩn đoán Sai lệch & Lỗ hổng Kiến trúc tại 3 Controller
| Vấn đề | Hiện trạng | Hệ quả |
|---|---|---|
| **Domain Event không được gắn** | Không có lệnh `ticket.AddDomainEvent(new CommentAddedEvent(...))` | `ItTmsDbContext.SaveChangesAsync` không kích hoạt `_domainEventDispatcher`. |
| **SignalR không phát sóng** | `SignalRNotificationDispatcher.HandleCommentAddedAsync` không chạy | Cả phòng `ticket_{id}` không nhận được sự kiện `ReceiveComment`. |
| **Notification không được tạo** | `NotificationObserver` không chạy | Đối tác trao đổi không nhận được chuông thông báo trong CSDL và SignalR. |
| **SystemLog bị bỏ sót** | `SystemLogObserver` không chạy | Mất dấu vết kiểm toán hành vi bình luận. |
| **Dữ liệu phản hồi Ajax thiếu DTO** | Chỉ trả về `{ success = true, message = "..." }` | Client JS không có `CommentDto` để render tức thì lên màn hình người gửi. |

---

## 2. KHẢO SÁT THỰC THỂ DOMAIN & SỰ KIỆN

### 2.1. `src/ItTms.Domain/Entities/Comment.cs`
- Kế thừa: `EntityBase<int>` (không phải `AggregateRoot<int>`, không tự quản lý Domain Events).
- Thuộc tính: `TicketId`, `UserId`, `Content`, `IsInternal`, `CreatedAt`, `Ticket`, `User`.
- Phương thức nhà máy: `Comment.Create(ticketId, userId, content, isInternal)` trả về `Result<Comment>`.
- Đánh giá: Thực thể thuần túy thuộc Aggregate của `Ticket`. Không nên biến `Comment` thành Aggregate Root độc lập nhằm tránh phá vỡ ranh giới Aggregate của DDD.

### 2.2. `src/ItTms.Domain/Entities/Ticket.cs`
- Kế thừa: `AggregateRoot<int>`, quản lý `_domainEvents` qua `AddDomainEvent(IDomainEvent)`.
- Hiện tại có các sự kiện: `TicketCreatedEvent`, `TicketAssignedEvent`, `TicketStatusChangedEvent`, `TicketClosedEvent`.
- **Thiếu sót trong Domain Model**: `Ticket.cs` chưa có phương thức nghiệp vụ đóng gói việc thêm bình luận (`AddComment`). Controller hiện đang tự gọi `Comment.Create` rồi add trực tiếp vào `DbContext`.

### 2.3. `src/ItTms.Domain/Events/CommentAddedEvent.cs`
```csharp
public sealed record CommentAddedEvent(
    int CommentId,
    int TicketId,
    int AuthorUserId,
    string Content,
    bool IsInternal,
    DateTime OccurredOn
) : IDomainEvent;
```
- Định nghĩa sự kiện hoàn chỉnh, mang đầy đủ ngữ cảnh để cả Observer tạo Notification, SystemLog và SignalR Dispatcher hoạt động.

---

## 3. KHẢO SÁT HẠ TẦNG REAL-TIME & CÁC OBSERVERS

### 3.1. Cơ chế điều phối 2 pha trong `ItTmsDbContext.SaveChangesAsync`
Tệp: `src/ItTms.Infrastructure/Persistence/ItTmsDbContext.cs` (Dòng 29 - 57)
```csharp
public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
{
    var entitiesWithEvents = ChangeTracker.Entries<IHasDomainEvents>()
        .Where(e => e.Entity.DomainEvents.Count > 0)
        .Select(e => e.Entity)
        .ToList();

    if (entitiesWithEvents.Count == 0 || _domainEventDispatcher == null)
        return await base.SaveChangesAsync(cancellationToken);

    var domainEvents = entitiesWithEvents.SelectMany(e => e.DomainEvents).ToList();
    entitiesWithEvents.ForEach(e => e.ClearDomainEvents());

    // Pha 1: In-Transaction Observers (Notification & SystemLog được add vào DbContext)
    await _domainEventDispatcher.DispatchInTransactionAsync(domainEvents, cancellationToken);

    // Commit toàn bộ xuống SQLite
    var result = await base.SaveChangesAsync(cancellationToken);

    // Pha 2: Post-Commit (SignalR WebSocket Push)
    await _domainEventDispatcher.DispatchPostCommitAsync(domainEvents, cancellationToken);

    return result;
}
```
**Phân tích điểm then chốt**:
- Vì `DispatchInTransactionAsync` diễn ra **trước** `base.SaveChangesAsync()`, các Observer như `NotificationObserver` và `SystemLogObserver` thêm các bản ghi `Notification` và `SystemLog` vào chính `DbContext` đó, sau đó tất cả được lưu trong cùng 1 transaction nguyên tử!
- Tuy nhiên, với SQLite AutoIncrement, nếu `CommentAddedEvent` được tạo trước khi `Comment` được commit lần đầu, `Comment.Id` sẽ bằng `0`!
- Do đó, cần có giải pháp để `CommentAddedEvent` có đúng `CommentId` thực tế khi bắn SignalR và ghi log.

### 3.2. `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs`
- Đã cài đặt đầy đủ phương thức `HandleCommentAddedAsync(CommentAddedEvent comment, CancellationToken)` (Dòng 131 - 164).
- Phân luồng nhóm:
  - Nếu `comment.IsInternal == true`: Đẩy `ReceiveComment` vào `ticket_{id}_internal` và `Ticket_{id}_internal`.
  - Nếu `comment.IsInternal == false`: Đẩy `ReceiveComment` vào `ticket_{id}` và `Ticket_{id}`.
- **Lỗ hổng phát hiện tại đây**: `HandleCommentAddedAsync` **chỉ đẩy sự kiện `ReceiveComment` vào nhóm Ticket**, mà **CHƯA gọi `SendToUserAsync`** để gửi chuông thông báo cá nhân (`ReceiveNotification` + `UpdateUnreadNotificationCount`) cho người nhận khi họ đang duyệt trang khác!

### 3.3. `src/ItTms.Web/Hubs/TicketHub.cs`
- `JoinTicketGroup(int ticketId)` (Dòng 66 - 118):
  - Phân quyền BOLA nghiêm ngặt (TC-07): Kiểm tra quyền của Admin, Leader, Assigned Staff và Creator.
  - Phân nhóm: Mọi người hợp lệ đều vào `ticket_{ticketId}`. Nhưng **chỉ Staff, Leader, Admin mới được gia nhập thêm `ticket_{ticketId}_internal`**.
- `SendComment(int ticketId, string message, bool isInternal = false)` (Dòng 135 - 198):
  - Phương thức này được viết cho việc gọi qua WebSocket direct invoke, tự động hạ cờ `isInternal = false` nếu User thường cố tình gửi cờ này. Tuy nhiên, giao diện hiện tại dùng Form HTTP POST nên phương thức này chưa được giao diện tận dụng.

### 3.4. `NotificationObserver` & `SystemLogObserver`
- `NotificationObserver.cs` (Dòng 142 - 182):
  - `if (domainEvent.IsInternal) return;` -> Bảo mật tuyệt đối: không bao giờ sinh thông báo cho User đối với ghi chú nội bộ.
  - Phân biệt tác giả: Nếu người gửi là User -> tạo Notification cho Assigned Staff. Nếu người gửi là Staff/Leader -> tạo Notification cho Creator.
- `SystemLogObserver.cs` (Dòng 75 - 90):
  - Ghi nhật ký phân biệt hành động `AddInternalNote` hoặc `AddComment` với nội dung trích đoạn.

---

## 4. MA TRẬN BẢO MẬT BÌNH LUẬN NỘI BỘ (`IsInternal = true`)

Hệ thống bảo vệ `IsInternal` vận hành qua 4 lớp độc lập:

| Lớp bảo vệ | Cơ chế kiểm soát | Vị trí mã nguồn | Trạng thái xác minh |
|---|---|---|---|
| **1. Ghi (Write)** | User Controller không có tham số `isInternal`, luôn truyền `isInternal: false`. Tại Hub, nếu User cố tình gửi cờ `isInternal=true`, hệ thống tự động cưỡng chế về `false`. | `TicketController.cs:277`<br>`TicketHub.cs:165` | Đã xác minh - Chống giả mạo 100% |
| **2. Đọc ban đầu (Read)** | Khi nạp trang `User/Ticket/Details`, truy vấn lọc cứng: `ticket.Comments.Where(c => !c.IsInternal)`. User không bao giờ tải được ghi chú nội bộ qua HTML. | `TicketController.cs:174` | Đã xác minh - Triệt tiêu rò rỉ dữ liệu |
| **3. Real-time Push** | Phân tách phòng SignalR: User chỉ ở phòng `ticket_{id}`, không ở phòng `ticket_{id}_internal`. Bình luận nội bộ chỉ bắn vào `ticket_{id}_internal`. | `TicketHub.cs:113`<br>`SignalRNotificationDispatcher.cs:152` | Đã xác minh - Đúng chuẩn phân lập kênh |
| **4. Thông báo chuông** | `NotificationObserver` có chốt chặn: `if (domainEvent.IsInternal) return;`. Tuyệt đối không sinh bản ghi thông báo trong DB gửi tới User. | `NotificationObserver.cs:145` | Đã xác minh - Chống rò rỉ qua Notification |

---

## 5. RÀ SOÁT BỘ KIỂM THỬ VÀ KHOẢNG TRỐNG KIỂM THỬ (TEST GAPS)

### 5.1. Hiện trạng thực tế
- Lệnh chạy kiểm thử: `dotnet test ItTms.sln`
- Kết quả: **336 passed, 0 failed, 0 skipped** (Thời gian chạy: 31 giây).
- 10 kịch bản kiểm thử nghiệm thu Bảng 7.2 (`Table72VerificationTests.cs`) đều vượt qua, bao gồm cả `TC08_UserCommentsOnResolvedTicket_IsBlockedAndNotSaved`.

### 5.2. Các khoảng trống kiểm thử nghiêm trọng (Test Gaps)
1. **Thiếu kiểm thử tích hợp cho Controller `AddComment`**: Hiện tại không có bài test nào gọi `controller.AddComment` và kiểm tra xem:
   - `CommentAddedEvent` có được phát sinh không.
   - Bản ghi `Notification` có được tự động tạo trong CSDL bởi `NotificationObserver` không.
   - Bản ghi `SystemLog` có được tạo bởi `SystemLogObserver` không.
2. **Thiếu kiểm thử phản hồi JSON của `AddComment` khi gọi qua Ajax**: Kiểm tra xem controller có trả về đầy đủ `CommentDto` khi có header `X-Requested-With: XMLHttpRequest` hay không.
3. **Thiếu kiểm thử phát sóng `ReceiveNotification` trong `SignalRNotificationDispatcher`**: Hiện tại `SignalRNotificationDispatcherTests` chỉ kiểm tra phát sóng `ReceiveComment` vào group `ticket_{id}`, chưa kiểm tra việc gửi thông báo chuông tới cá nhân đối tác qua `SendToUserAsync`.

---

## 6. KHẢO SÁT CLIENT-SIDE JAVASCRIPT & PHÍM TẮT ENTER

### 6.1. `src/ItTms.Web/wwwroot/js/ticket-realtime.js`
1. **Lỗi phím Enter**:
   - Khung nhập bình luận là thẻ `<textarea id="comment-content-input">` với placeholder `"(Enter gửi)"`.
   - Trong file JS hoàn toàn **chưa có sự kiện `keydown`** bắt phím `Enter` (và kiểm tra `!e.shiftKey`) để gọi submit form, dẫn tới phím Enter chỉ tạo dòng mới.
2. **Lỗi hiển thị tức thì & Trùng lặp**:
   - Sau khi `fetch(commentForm.action, ...)` thành công, hàm chỉ xóa ô nhập `commentInput.value = ""` mà không hiển thị bình luận lên giao diện, vì đang chờ sự kiện `ReceiveComment` từ SignalR (vốn bị nghẽn ở backend).
   - Hàm `appendCommentToView` thiếu thuộc tính `data-comment-id="${comment.id}"` trên thẻ bao bọc và thiếu logic kiểm tra trùng lặp:
     ```javascript
     if (comment.id && commentList.querySelector(`[data-comment-id="${comment.id}"]`)) return;
     ```
3. **Sử dụng `alert(...)` mặc định**:
   - Dòng 104 và 108 đang dùng `alert(...)` của trình duyệt thay vì Floating Card Box.

### 6.2. `src/ItTms.Web/wwwroot/js/signalr-notification.js`
- Dòng 38 dùng `alert("Thông báo bảo mật: " + reason);` khi bị ForceLogout.
- Dòng 64 dùng icon thẻ `<i class="bi bi-bell-fill me-2"></i>` (phụ thuộc font icon Bootstrap Icons dễ bị lỗi hiển thị).
- Dòng 61 hiển thị Toast dạng 1 dòng thô sơ, chưa đạt chuẩn Floating Card Box nhiều tầng có thanh tiến trình tự tắt.

---

## 7. BẢN THIẾT KẾ KIẾN TRÚC ĐỀ XUẤT (RECOMMENDED BLUEPRINT)

### 7.1. Chuẩn hóa Backend xử lý `AddComment` trên cả 3 Controller
Tại `User/TicketController.cs`, `Staff/StaffTicketController.cs`, `Leader/LeaderTicketController.cs`:
Sau khi kiểm tra nghiệp vụ và tạo `Comment`:
```csharp
// 1. Thêm comment và commit lần 1 để SQLite sinh Id tự tăng
await _dbContext.Comments.AddAsync(commentResult.Value);
await _dbContext.SaveChangesAsync();

// 2. Gắn Domain Event vào Ticket với đúng CommentId vừa sinh
var commentEvent = new CommentAddedEvent(
    commentResult.Value.Id,
    ticket.Id,
    CurrentUserId.Value,
    commentResult.Value.Content,
    commentResult.Value.IsInternal,
    commentResult.Value.CreatedAt);

ticket.AddDomainEvent(commentEvent);

// 3. Commit lần 2: Kích hoạt NotificationObserver, SystemLogObserver (In-Transaction) 
//    và SignalRNotificationDispatcher (Post-Commit)
await _dbContext.SaveChangesAsync();

// 4. Trả về DTO hoàn chỉnh cho Ajax request
if (IsAjaxRequest)
{
    var author = await _dbContext.Users.AsNoTracking()
        .Select(u => new { u.FullName, u.Role })
        .FirstOrDefaultAsync(u => u.Id == CurrentUserId.Value);

    var commentDto = new
    {
        id = commentResult.Value.Id,
        ticketId = ticket.Id,
        authorUserId = CurrentUserId.Value,
        authorName = author?.FullName ?? User.Identity?.Name ?? "Người dùng",
        authorRole = author?.Role.ToString() ?? "User",
        content = commentResult.Value.Content,
        isInternal = commentResult.Value.IsInternal,
        createdAt = commentResult.Value.CreatedAt,
        formattedTime = "Vừa xong"
    };

    return Ok(new
    {
        success = true,
        message = isInternal ? "Đã lưu ghi chú nội bộ." : "Bình luận đã gửi thành công.",
        comment = commentDto
    });
}
```

### 7.2. Hoàn thiện `SignalRNotificationDispatcher.HandleCommentAddedAsync`
Bổ sung luồng đẩy thông báo chuông cá nhân cho đối tác:
```csharp
// Nếu là bình luận công khai, gửi thông báo chuông tới đối tác qua SendToUserAsync
if (!comment.IsInternal)
{
    var ticket = await _dbContext.Tickets
        .AsNoTracking()
        .Select(t => new { t.Id, t.CreatorId, t.AssignedToId, t.Title })
        .FirstOrDefaultAsync(t => t.Id == comment.TicketId, cancellationToken);

    if (ticket != null)
    {
        int? recipientId = comment.AuthorUserId == ticket.CreatorId 
            ? ticket.AssignedToId 
            : ticket.CreatorId;

        if (recipientId.HasValue)
        {
            var authorName = author?.FullName ?? "Đối tác";
            var targetLink = recipientId.Value == ticket.CreatorId 
                ? $"/User/Ticket/Details/{ticket.Id}" 
                : $"/Staff/Ticket/Details/{ticket.Id}";

            await SendToUserAsync(
                recipientId.Value, 
                $"[{authorName}] đã gửi bình luận trên ticket #{ticket.Id}: {ticket.Title}", 
                targetLink, 
                cancellationToken);
        }
    }
}
```

### 7.3. Cải tiến Frontend `ticket-realtime.js`
1. Bắt phím Enter:
   ```javascript
   commentInput.addEventListener("keydown", function (e) {
       if (e.key === "Enter" && !e.shiftKey) {
           e.preventDefault();
           if (!commentBtn.disabled && commentInput.value.trim().length > 0) {
               commentForm.requestSubmit ? commentForm.requestSubmit() : commentForm.dispatchEvent(new Event("submit", { cancelable: true }));
           }
       }
   });
   ```
2. Hiển thị tức thì từ phản hồi Ajax và chống trùng lặp:
   ```javascript
   if (response.ok) {
       const resData = await response.json().catch(() => ({}));
       if (resData.comment) {
           appendCommentToView(resData.comment);
           scrollToBottom();
       }
       commentInput.value = "";
       if (isInternalCheckbox) isInternalCheckbox.checked = false;
   }
   ```
3. Trong `appendCommentToView(comment)`:
   - Gắn `data-comment-id="${comment.id}"` vào thẻ bọc bong bóng chat.
   - Thêm câu lệnh kiểm tra trùng lặp ở đầu hàm:
     ```javascript
     if (comment.id && commentList.querySelector(`[data-comment-id="${comment.id}"]`)) {
         return; // Đã hiển thị trước đó, bỏ qua
     }
     ```
4. Đồng bộ trong các View Razor (`User/Ticket/Details`, `Staff/StaffTicket/Details`, `Leader/LeaderTicket/Details`):
   Gắn thuộc tính `data-comment-id="@c.Id"` vào từng thẻ comment render sẵn bởi máy chủ.

### 7.4. Kế hoạch Bổ sung Kiểm thử Tự động
1. Thêm test class `TicketCommentDomainEventIntegrationTests.cs` kiểm tra:
   - Gửi comment qua `TicketController.AddComment` phát sinh đúng `CommentAddedEvent`.
   - `NotificationObserver` ghi nhận bản ghi `Notification` trong DB với đúng `UserId` người nhận.
   - `SystemLogObserver` ghi nhận bản ghi `SystemLog` với đúng action `AddComment` hoặc `AddInternalNote`.
   - Ajax request trả về mã `200` kèm payload `comment` có đúng `id > 0`.
2. Bổ sung bài kiểm thử trong `SignalRNotificationDispatcherTests.cs` xác nhận khi có `CommentAddedEvent`, hàm vừa broadcast vào room ticket vừa gọi `SendToUserAsync` cho đối tác.
