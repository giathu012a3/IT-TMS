# BÁO CÁO KHẢO SÁT & BẢN THIẾT KẾ KIẾN TRÚC FRONTEND JS, TOAST NOTIFICATION & TEST BASELINE (IT-TMS)

> **Người thực hiện**: Frontend JS & Tests Explorer (`survey_explorer_3`)  
> **Thời điểm khảo sát**: 2026-10-25T13:27:00Z  
> **Phạm vi khảo sát**: `src/ItTms.Web/wwwroot/js/`, `src/ItTms.Web/Views/Shared/`, `src/ItTms.Web/wwwroot/css/`, Controllers xử lý bình luận (`TicketController`, `StaffTicketController`, `LeaderTicketController`), và bộ kiểm thử `tests/ItTms.Tests/`.

---

## 1. TỔNG QUAN HIỆN TRẠNG & BẰNG CHỨNG THỰC TẾ (FACT-BASED AUDIT)

### 1.1. Hiện trạng `src/ItTms.Web/wwwroot/js/ticket-realtime.js`
Tập tin hiện tại có 167 dòng, quản lý kết nối SignalR nhóm `ticket_{id}` và luồng bình luận. Qua kiểm tra chi tiết phát hiện các điểm nghẽn nghiêm trọng sau:

1. **Thiếu hoàn toàn trình bắt sự kiện phím `Enter`**:
   - Khai báo phần tử: `const commentInput = document.getElementById("comment-content-input");` (dòng 15).
   - Khung giao diện HTML tại cả 3 Views (`User/Ticket/Details`, `Staff/StaffTicket/Details`, `Leader/LeaderTicket/Details`) dùng thẻ `<textarea placeholder="... (Enter gửi)">`.
   - **Thực tế**: Trong toàn bộ file `ticket-realtime.js` chỉ có `commentForm.addEventListener("submit", ...)` (dòng 77), **hoàn toàn không có** `addEventListener("keydown")` trên `commentInput`. Khi người dùng gõ nội dung và nhấn `Enter`, trình duyệt mặc định chèn ký tự xuống dòng `\n` thay vì gửi tin nhắn.
   - **Yêu cầu bổ sung**: Cần bắt sự kiện `keydown` phím `Enter` (khi không giữ phím `Shift` và `!e.isComposing` để tương thích bộ gõ tiếng Việt Telex/IME), ngăn chặn hành vi mặc định `e.preventDefault()`, kiểm tra chuỗi không rỗng và kích hoạt `commentForm.requestSubmit()`.

2. **Cơ chế hiển thị bình luận, Chống trùng lặp (Deduplication) & Cập nhật tức thì (Optimistic UI)**:
   - Hàm `appendCommentToView(comment)` (dòng 116–152):
     - Sinh mã HTML dạng thẻ `div` bong bóng chat nhưng **không gán thuộc tính `data-comment-id="${comment.id}"`**.
     - Không có bước kiểm tra trùng lặp: nếu sự kiện `ReceiveComment` được bắn về hoặc client gọi hiển thị trước, hàm sẽ chèn thêm phần tử mới vào DOM gây lặp tin nhắn (duplicate).
     - Cả 3 Razor Views (`User/Ticket/Details.cshtml:190`, `StaffTicket/Details.cshtml:204`, `LeaderTicket/Details.cshtml:185`) khi render danh sách bình luận ban đầu từ Server cũng **thiếu `data-comment-id="@c.Id"`**.
   - Khi gửi form thành công (`response.ok` tại dòng 99):
     - Đoạn mã chỉ thực hiện: `commentInput.value = "";` rồi kết thúc! Nó hoàn toàn không render nội dung bình luận vừa gửi lên màn hình người gửi mà thụ động chờ sự kiện `ReceiveComment` từ SignalR.
     - Tuy nhiên, do Backend Controller chưa phát sự kiện SignalR (xem mục 1.3), người gửi thấy ô nhập bị xóa nhưng tin nhắn biến mất, không xuất hiện trên giao diện.

3. **Tồn dư hộp thoại `alert(...)` mặc định của trình duyệt**:
   - Dòng 104: `alert(errData.message || "Không thể gửi bình luận. Vui lòng thử lại.");`
   - Dòng 108: `alert("Lỗi kết nối máy chủ khi gửi phản hồi.");`
   - Cả 2 điểm đều sử dụng hộp thoại modal chặn luồng (blocking) nguyên thủy của trình duyệt, vi phạm tiêu chuẩn trải nghiệm người dùng doanh nghiệp.

4. **Vi phạm quy chuẩn Icon (Sử dụng `<i class="bi ...">` thay vì SVG)**:
   - Dòng 65: Khối thông báo khóa bình luận khi trạng thái ticket chuyển sang không cho phép (Resolved/Closed/Rejected):
     ```javascript
     commentLockNotice.innerHTML = `
         <div class="alert alert-warning py-2 mb-0">
             <i class="bi bi-lock-fill me-1"></i> Yêu cầu hiện ở trạng thái <strong>${data.newStatusLabel}</strong>. Chức năng gửi bình luận đã bị khóa.
         </div>`;
     ```
     Đang dùng thẻ `<i class="bi bi-lock-fill me-1"></i>` phụ thuộc vào font icon Bootstrap Icons (dễ bị lỗi font hiển thị ô vuông), thay vì dùng SVG sprite `<svg class="bi me-1" width="16" height="16"><use xlink:href="#lock-fill"/></svg>`.

---

### 1.2. Hiện trạng `src/ItTms.Web/wwwroot/js/signalr-notification.js`
Tập tin hiện tại có 102 dòng, quản lý kết nối toàn cục tới `/hubs/ticket`.

1. **Hiển thị thông báo Toast thô sơ (1 dòng, màu xanh đặc, thiếu cấu trúc đa tầng)**:
   - Hàm `showBootstrapToast(message, link)` (dòng 55–76):
     ```javascript
     const toastHtml = `
         <div id="${toastId}" class="toast align-items-center text-bg-primary border-0 mb-2" role="alert" aria-live="assertive" aria-atomic="true">
             <div class="d-flex">
                 <div class="toast-body">
                     <i class="bi bi-bell-fill me-2"></i> ${escapeHtml(message)}
                 </div>
                 ${link ? `<a href="${link}" class="btn btn-sm btn-light align-self-center me-2">Xem</a>` : ''}
                 <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
             </div>
         </div>`;
     ```
     - Định dạng: Toast 1 dòng phẳng viền phẳng, màu xanh Bootstrap `text-bg-primary`, không có tiêu đề, không có dải màu phân loại ngữ nghĩa, không có thanh tiến trình đếm ngược (countdown bar).
     - Icon: Sử dụng `<i class="bi bi-bell-fill me-2"></i>` (Bootstrap Icons webfont), vi phạm quy chuẩn SVG sprite.
2. **Tồn dư hộp thoại `alert(...)` mặc định**:
   - Dòng 38 trong bộ xử lý `ForceLogout`:
     ```javascript
     connection.on("ForceLogout", function (reason) {
         alert("Thông báo bảo mật: " + reason);
         window.location.href = "/Auth/Login?error=" + encodeURIComponent(reason);
     });
     ```
     Sử dụng `alert(...)` chặn luồng màn hình. Cần chuyển sang Floating Card Box hoặc điều hướng có kèm thông báo chuẩn hóa.

---

### 1.3. Hiện trạng Backend Controllers & Luồng sự kiện Domain Event (`AddComment`)
Khảo sát mã nguồn tại 3 Controllers:
- `src/ItTms.Web/Areas/User/Controllers/TicketController.cs:241`
- `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs:251`
- `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs:337`

**Nguyên nhân cốt lõi khiến SignalR không phát tin nhắn sang người nhận**:
```csharp
// Đoạn mã hiện tại trong cả 3 Controllers:
var commentResult = Comment.Create(ticket.Id, CurrentUserId.Value, content, isInternal);
await _dbContext.Comments.AddAsync(commentResult.Value);
await _dbContext.SaveChangesAsync();

if (IsAjaxRequest)
    return Ok(new { success = true, message = "Bình luận đã gửi." });
```
1. **Không kích hoạt Domain Event**: `ticket.AddDomainEvent(new CommentAddedEvent(...))` hoàn toàn không được gọi trước hoặc sau `SaveChangesAsync()`.
   - Kết quả: `ItTmsDbContext.SaveChangesAsync` kiểm tra `ChangeTracker.Entries<IHasDomainEvents>()` thấy danh sách rỗng, do đó `InMemoryDomainEventDispatcher` không chạy.
   - `SignalRNotificationDispatcher.HandleCommentAddedAsync` **không bao giờ được kích hoạt**!
   - `NotificationObserver` và `SystemLogObserver` **không bao giờ được kích hoạt**!
   - Hệ quả: Người nhận trong cùng phòng ticket không nhận được sự kiện `ReceiveComment`, và người nhận không có mặt trong phòng không nhận được chuông thông báo `ReceiveNotification`.
2. **Response JSON không trả về dữ liệu bình luận (`CommentDto`)**:
   - Controller chỉ trả về `{ success = true, message = "..." }`.
   - Client JS không có dữ liệu để render tức thì (optimistic update) với đầy đủ thông tin: `id`, `authorName`, `authorRole`, `formattedTime`, `content`, `isInternal`.

---

### 1.4. Hiện trạng Notification Markup (`_ToastAlerts.cshtml`) & Layout
1. `_ToastAlerts.cshtml` (38 dòng):
   - Đang sử dụng các khối alert Bootstrap tĩnh trải ngang:
     ```html
     <div class="alert alert-success alert-dismissible fade show shadow-sm border-0 d-flex align-items-center py-2 px-3 mb-3 rounded-3 small" role="alert">
         <svg class="bi flex-shrink-0 me-2" width="18" height="18" fill="currentColor"><use xlink:href="#check-circle-fill"/></svg>
         <div class="flex-grow-1">
             <strong>Thành công:</strong> @TempData["SuccessMessage"]
         </div>
     ...
     ```
2. Vị trí render trong `_Layout.cshtml` (dòng 310–324):
   - Thẻ `#toast-container` nằm riêng biệt ở góc trên bên phải: `<div id="toast-container" class="position-fixed top-0 end-0 p-3" style="z-index: 1090;"></div>`.
   - Trong khi đó, `@await Html.PartialAsync("_ToastAlerts")` lại được đặt bên trong thẻ `<main>` hoặc ngay trên `.auth-split-shell`.
   - **Hậu quả**: Khi có thông báo từ `TempData`, nó xuất hiện dạng thanh banner chiếm trọn bề ngang ở đầu trang, đẩy toàn bộ nội dung bàn làm việc xuống dưới; trên trang `Login.cshtml`, nó phá vỡ cấu trúc lưới chia 2 cột toàn màn hình (`.auth-split-shell`).

---

### 1.5. Kiểm thử Baseline (Test Suite Baseline)
Đã chạy toàn bộ bộ kiểm thử tự động của hệ thống qua lệnh:
```powershell
dotnet test c:\IT_System\ItTms.sln
```
**Kết quả thực tế**:
- **Trạng thái**: PASSED 100% (Mã thoát 0).
- **Tổng số bài test**: **336 tests passed**, 0 failed, 0 skipped.
- **Thời gian chạy**: ~31 giây.
- **Phủ sóng**:
  - `Table72VerificationTests`: Nghiệm thu trọn vẹn 10 kịch bản Bảng 7.2 (`TC-01` đến `TC-10`).
  - `StateMachine49PermutationTests`: Kiểm tra 49 hoán vị chuyển đổi trạng thái của State Pattern.
  - `TicketHubLifecycleAndBolaAdversarialTests`: Kiểm tra kết nối SignalR, phân quyền nhóm phòng ticket và chống xâm nhập BOLA/IDOR.
  - `ObserverPatternTests`: Kiểm thử cơ chế Domain Events cho Notification và SystemLog.
  - `AdversarialConcurrencyStressTests` & `OptimisticConcurrencyTests`: Kiểm thử tranh chấp cập nhật đồng thời qua ConcurrencyToken.
- **Lỗ hổng kiểm thử cần bổ sung (Test Gap)**:
  - Hiện chưa có bài test ở tầng Controller (`TicketController`, `StaffTicketController`, `LeaderTicketController`) để xác minh rằng khi gọi `AddComment(...)`, sự kiện `CommentAddedEvent` được phát đi và thông báo SignalR / chuông thông báo được kích hoạt tới đúng đối tác.

---

## 2. BẢN THIẾT KẾ KIẾN TRÚC HỆ THỐNG THÔNG BÁO FLOATING CARD BOX

Hệ thống thông báo mới được thiết kế thống nhất cho cả **Server-side Flash Messages** (`TempData`), **Client-side JavaScript**, và **Real-time SignalR Pushes**.

### 2.1. Cấu trúc Hình học & Phân tầng của Thẻ Nổi (Multi-Layer Card Architecture)
Mỗi hộp thẻ nổi (Floating Card Box) bao gồm 6 lớp thành phần theo chiều ngang và dọc:
```
+---+---------------------------------------------------------+---+
|   | [Icon Box]  [Title: Phân loại]                   [Close]|   |
| S |             [Content: Nội dung chi tiết thông báo]      |   |
| T |             [Link: Hành động (nếu có)]                  |   |
| R +---------------------------------------------------------+   |
| I | [==== Thanh tiến trình đếm ngược 5s (Hover Pause) ====] |   |
+---+-------------------------------------------------------------+
```
1. **Container neo cố định (`#toast-container`)**:
   - Vị trí: `position: fixed; top: 1.25rem; right: 1.25rem; z-index: 1100;`
   - Chiều rộng: `width: 380px; max-width: calc(100vw - 2.5rem);`
   - Bố cục: `display: flex; flex-direction: column; gap: 0.75rem; pointer-events: none;`
   - Xếp chồng mượt mà: Các thẻ mới chèn lên trên (`afterbegin` hoặc `beforeend`), thẻ con bật `pointer-events: auto;`.
2. **Khung thẻ nổi (`.toast-card-box`)**:
   - Nền: `var(--surface)` kết hợp hiệu ứng viền hairline `1px solid var(--border)` và đổ bóng nổi đa tầng `var(--shadow-float)`.
   - Bo góc: `10px` (`--radius-card`).
   - Hiệu ứng xuất hiện: Trượt vào từ phải sang trái kết hợp mờ dần (`translateX(100%) -> translateX(0)`, `opacity: 0 -> 1` trong 220ms).
   - Hiệu ứng biến mất: Mờ dần và co độ cao về 0 (`opacity: 0`, `max-height: 0`, `margin: 0`, `padding: 0` trong 200ms).
3. **Dải màu chỉ thị ngữ nghĩa bên cạnh (`.toast-card-stripe`)**:
   - Dải viền màu dọc 4px ở mép trái:
     - `Success`: `var(--positive)` (`#15803d` ở Light, `#4ade80` ở Dark).
     - `Error`: `var(--negative)` (`#b91c1c` ở Light, `#f87171` ở Dark).
     - `Warning`: `var(--warning)` (`#b45309` ở Light, `#fbbf24` ở Dark).
     - `Info` / `SignalR`: `var(--accent)` (`#2563eb` ở Light, `#60a5fa` ở Dark).
4. **Khối Icon SVG trạng thái (`.toast-card-icon-box`)**:
   - Kích thước: `36px x 36px`, bo góc tròn `50%` hoặc bo góc nhẹ `8px`.
   - Nền: Dùng màu nền pill tương ứng (`var(--pill-*-bg)`).
   - Biểu tượng: Sử dụng 100% SVG sprite từ `_SvgIcons.cshtml`:
     - `Success`: `#check-circle-fill`
     - `Error`: `#x-circle-fill` hoặc `#exclamation-triangle-fill`
     - `Warning`: `#exclamation-triangle-fill`
     - `Info` / `SignalR`: `#bell-fill`
5. **Khối Nội dung (`.toast-card-content`)**:
   - **Tiêu đề phân loại (`.toast-card-title`)**: Chữ đậm (`font-weight: 600`), kích thước 13.5px, màu `var(--text)`.
   - **Nội dung mô tả (`.toast-card-message`)**: Kích thước 12.5px, màu `var(--muted)`, ngắt dòng thông minh (`word-break: break-word`), độ giãn dòng 1.45.
   - **Nút liên kết điều hướng (nếu có `link`)**: Nút nhỏ gọn `btn btn-sm btn-outline-secondary` mở chi tiết ticket tương ứng.
6. **Nút đóng (`.toast-card-close`)**:
   - Kích thước `24px x 24px`, căn góc trên bên phải, biểu tượng dấu đóng SVG hoặc dấu `×`, màu `var(--faint)`, hover đổi sang `var(--text)`.
7. **Thanh tiến trình đếm ngược & Cơ chế Hover Pause (`.toast-card-progress`)**:
   - Đặt ở đáy thẻ, chiều cao `3px`, nền mờ `rgba(0,0,0,0.06)` hoặc Dark mode `rgba(255,255,255,0.06)`.
   - Vạch tiến trình đếm ngược `.toast-card-progress-bar`: Chạy từ `100%` về `0%` trong vòng 5 giây (`5000ms`) thông qua CSS `@keyframes toastCountdown`.
   - **Cơ chế Hover Pause**:
     ```css
     .toast-card-box:hover .toast-card-progress-bar {
       animation-play-state: paused;
     }
     ```
     Khi người dùng rê chuột vào hộp thẻ để đọc nội dung dài, thanh tiến trình lập tức dừng lại, thẻ không bị đóng. Khi chuột rời khỏi thẻ, tiến trình tiếp tục đếm ngược.
   - Khi thanh tiến trình chạm mốc kết thúc (`animationend`), thẻ tự động kích hoạt hàm đóng và biến mất khỏi DOM.

---

### 2.2. Chi tiết Mã Nguồn Đề xuất cho Bộ CSS (`site.css`)
Thêm các định nghĩa CSS sau vào cuối `src/ItTms.Web/wwwroot/css/site.css`:

```css
/* ==========================================================================
   FLOATING CARD BOX NOTIFICATION SYSTEM (ENTERPRISE TOAST STACKING)
   ========================================================================== */
.toast-card-container {
  position: fixed;
  top: 1.25rem;
  right: 1.25rem;
  z-index: 1100;
  width: 380px;
  max-width: calc(100vw - 2.5rem);
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  pointer-events: none;
}

.toast-card-box {
  pointer-events: auto;
  position: relative;
  display: flex;
  flex-direction: column;
  background-color: var(--surface);
  border: 1px solid var(--border);
  border-radius: var(--radius-card);
  box-shadow: var(--shadow-float);
  overflow: hidden;
  transition: transform 220ms cubic-bezier(0.16, 1, 0.3, 1), opacity 200ms ease, max-height 200ms ease, margin 200ms ease, padding 200ms ease;
  animation: toastCardSlideIn 250ms cubic-bezier(0.16, 1, 0.3, 1) forwards;
}

@keyframes toastCardSlideIn {
  from {
    transform: translateX(110%);
    opacity: 0;
  }
  to {
    transform: translateX(0);
    opacity: 1;
  }
}

.toast-card-box.toast-closing {
  opacity: 0;
  transform: translateY(-8px) scale(0.97);
  max-height: 0 !important;
  margin-bottom: 0 !important;
  padding-top: 0 !important;
  padding-bottom: 0 !important;
  border-width: 0 !important;
}

.toast-card-inner {
  display: flex;
  align-items: flex-start;
  padding: 1rem 1rem 0.85rem 0.9rem;
  gap: 0.85rem;
  position: relative;
}

/* 4px Semantic Left Stripe */
.toast-card-stripe {
  position: absolute;
  top: 0;
  left: 0;
  bottom: 0;
  width: 4px;
}

.toast-card-icon-box {
  width: 36px;
  height: 36px;
  min-width: 36px;
  border-radius: 9px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  border: 1px solid transparent;
}

.toast-card-content {
  flex: 1 1 auto;
  min-width: 0;
}

.toast-card-title {
  font-size: 13.5px;
  font-weight: 600;
  letter-spacing: -0.01em;
  color: var(--text);
  margin-bottom: 2px;
}

.toast-card-message {
  font-size: 12.5px;
  color: var(--muted);
  line-height: 1.45;
  word-break: break-word;
}

.toast-card-actions {
  margin-top: 0.5rem;
  display: flex;
  gap: 0.5rem;
}

.toast-card-close {
  background: transparent;
  border: 0;
  color: var(--faint);
  padding: 2px;
  cursor: pointer;
  border-radius: 4px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  transition: color 120ms ease, background-color 120ms ease;
}

.toast-card-close:hover {
  color: var(--text);
  background-color: var(--subtle);
}

/* 5s Countdown Progress Bar */
.toast-card-progress {
  height: 3px;
  width: 100%;
  background-color: var(--rule);
  overflow: hidden;
}

.toast-card-progress-bar {
  height: 100%;
  width: 100%;
  animation: toastCountdown 5000ms linear forwards;
}

.toast-card-box:hover .toast-card-progress-bar {
  animation-play-state: paused;
}

@keyframes toastCountdown {
  from { width: 100%; }
  to { width: 0%; }
}

/* Color Tokens per Semantic Variant */
.toast-card-success .toast-card-stripe { background-color: var(--positive); }
.toast-card-success .toast-card-icon-box {
  background-color: var(--pill-positive-bg);
  color: var(--pill-positive-ink);
  border-color: var(--pill-positive-border);
}
.toast-card-success .toast-card-progress-bar { background-color: var(--positive); }

.toast-card-error .toast-card-stripe { background-color: var(--negative); }
.toast-card-error .toast-card-icon-box {
  background-color: var(--pill-negative-bg);
  color: var(--pill-negative-ink);
  border-color: var(--pill-negative-border);
}
.toast-card-error .toast-card-progress-bar { background-color: var(--negative); }

.toast-card-warning .toast-card-stripe { background-color: var(--warning); }
.toast-card-warning .toast-card-icon-box {
  background-color: var(--pill-warning-bg);
  color: var(--pill-warning-ink);
  border-color: var(--pill-warning-border);
}
.toast-card-warning .toast-card-progress-bar { background-color: var(--warning); }

.toast-card-info .toast-card-stripe { background-color: var(--accent); }
.toast-card-info .toast-card-icon-box {
  background-color: var(--pill-info-bg);
  color: var(--pill-info-ink);
  border-color: var(--pill-info-border);
}
.toast-card-info .toast-card-progress-bar { background-color: var(--accent); }
```

---

### 2.3. Cải tiến `_ToastAlerts.cshtml` Đồng bộ với Khung Nổi
Đưa `_ToastAlerts.cshtml` trực tiếp vào trong `<div id="toast-container" class="toast-card-container">` trong `_Layout.cshtml`:

```razor
@* _ToastAlerts.cshtml: Đồng bộ thông báo TempData và ModelState dạng Floating Card Box *@
@if (TempData["SuccessMessage"] != null || TempData["ErrorMessage"] != null || TempData["WarningMessage"] != null || !ViewData.ModelState.IsValid)
{
    @if (TempData["SuccessMessage"] != null)
    {
        <div class="toast-card-box toast-card-success" role="alert" aria-live="assertive">
            <div class="toast-card-stripe"></div>
            <div class="toast-card-inner">
                <div class="toast-card-icon-box">
                    <svg class="bi" width="18" height="18"><use xlink:href="#check-circle-fill"/></svg>
                </div>
                <div class="toast-card-content">
                    <div class="toast-card-title">Thành công</div>
                    <div class="toast-card-message">@TempData["SuccessMessage"]</div>
                </div>
                <button type="button" class="toast-card-close" aria-label="Đóng" onclick="window.dismissToastCard(this.closest('.toast-card-box'))">
                    <svg class="bi" width="14" height="14"><use xlink:href="#x-circle-fill"/></svg>
                </button>
            </div>
            <div class="toast-card-progress">
                <div class="toast-card-progress-bar"></div>
            </div>
        </div>
    }

    @if (TempData["ErrorMessage"] != null)
    {
        <div class="toast-card-box toast-card-error" role="alert" aria-live="assertive">
            <div class="toast-card-stripe"></div>
            <div class="toast-card-inner">
                <div class="toast-card-icon-box">
                    <svg class="bi" width="18" height="18"><use xlink:href="#x-circle-fill"/></svg>
                </div>
                <div class="toast-card-content">
                    <div class="toast-card-title">Thông báo lỗi</div>
                    <div class="toast-card-message">@TempData["ErrorMessage"]</div>
                </div>
                <button type="button" class="toast-card-close" aria-label="Đóng" onclick="window.dismissToastCard(this.closest('.toast-card-box'))">
                    <svg class="bi" width="14" height="14"><use xlink:href="#x-circle-fill"/></svg>
                </button>
            </div>
            <div class="toast-card-progress">
                <div class="toast-card-progress-bar"></div>
            </div>
        </div>
    }

    @if (TempData["WarningMessage"] != null)
    {
        <div class="toast-card-box toast-card-warning" role="alert" aria-live="assertive">
            <div class="toast-card-stripe"></div>
            <div class="toast-card-inner">
                <div class="toast-card-icon-box">
                    <svg class="bi" width="18" height="18"><use xlink:href="#exclamation-triangle-fill"/></svg>
                </div>
                <div class="toast-card-content">
                    <div class="toast-card-title">Lưu ý</div>
                    <div class="toast-card-message">@TempData["WarningMessage"]</div>
                </div>
                <button type="button" class="toast-card-close" aria-label="Đóng" onclick="window.dismissToastCard(this.closest('.toast-card-box'))">
                    <svg class="bi" width="14" height="14"><use xlink:href="#x-circle-fill"/></svg>
                </button>
            </div>
            <div class="toast-card-progress">
                <div class="toast-card-progress-bar"></div>
            </div>
        </div>
    }

    @if (!ViewData.ModelState.IsValid && ViewData.ModelState[string.Empty]?.Errors.Count > 0)
    {
        <div class="toast-card-box toast-card-error" role="alert" aria-live="assertive">
            <div class="toast-card-stripe"></div>
            <div class="toast-card-inner">
                <div class="toast-card-icon-box">
                    <svg class="bi" width="18" height="18"><use xlink:href="#exclamation-triangle-fill"/></svg>
                </div>
                <div class="toast-card-content">
                    <div class="toast-card-title">Xác thực không hợp lệ</div>
                    <div class="toast-card-message">
                        @foreach (var err in ViewData.ModelState[string.Empty]!.Errors)
                        {
                            <div>@err.ErrorMessage</div>
                        }
                    </div>
                </div>
                <button type="button" class="toast-card-close" aria-label="Đóng" onclick="window.dismissToastCard(this.closest('.toast-card-box'))">
                    <svg class="bi" width="14" height="14"><use xlink:href="#x-circle-fill"/></svg>
                </button>
            </div>
            <div class="toast-card-progress">
                <div class="toast-card-progress-bar"></div>
            </div>
        </div>
    }
}
```

---

### 2.4. Trình Điều khiển Toast JavaScript Toàn cục (`site.js`)
Cung cấp API `window.showToastCard({ type, title, message, link, duration })` và tự động gắn kết sự kiện đóng cho các card được render từ Server:

```javascript
// Gắn kết vào window để mọi file JS (signalr-notification.js, ticket-realtime.js) gọi trực tiếp:
window.dismissToastCard = function (card) {
    if (!card || card.classList.contains("toast-closing")) return;
    card.classList.add("toast-closing");
    setTimeout(function () {
        if (card.parentNode) card.parentNode.removeChild(card);
    }, 220);
};

window.showToastCard = function (options) {
    const container = document.getElementById("toast-container");
    if (!container) return;

    const type = options.type || "info"; // success | error | warning | info
    const title = options.title || (type === "success" ? "Thành công" : type === "error" ? "Thông báo lỗi" : type === "warning" ? "Cảnh báo" : "Thông báo");
    const message = options.message || "";
    const link = options.link || null;
    const duration = options.duration !== undefined ? options.duration : 5000;

    let iconSymbol = "#bell-fill";
    if (type === "success") iconSymbol = "#check-circle-fill";
    else if (type === "error") iconSymbol = "#x-circle-fill";
    else if (type === "warning") iconSymbol = "#exclamation-triangle-fill";

    const card = document.createElement("div");
    card.className = `toast-card-box toast-card-${type}`;
    card.setAttribute("role", "alert");
    card.setAttribute("aria-live", "assertive");

    card.innerHTML = `
        <div class="toast-card-stripe"></div>
        <div class="toast-card-inner">
            <div class="toast-card-icon-box">
                <svg class="bi" width="18" height="18"><use xlink:href="${iconSymbol}"/></svg>
            </div>
            <div class="toast-card-content">
                <div class="toast-card-title">${escapeHtml(title)}</div>
                <div class="toast-card-message">${escapeHtml(message)}</div>
                ${link ? `<div class="toast-card-actions"><a href="${link}" class="btn btn-sm btn-outline-secondary py-0 px-2 font-monospace" style="font-size: 11.5px;">Xem chi tiết &rarr;</a></div>` : ''}
            </div>
            <button type="button" class="toast-card-close" aria-label="Đóng">
                <svg class="bi" width="14" height="14"><use xlink:href="#x-circle-fill"/></svg>
            </button>
        </div>
        <div class="toast-card-progress">
            <div class="toast-card-progress-bar" style="animation-duration: ${duration}ms;"></div>
        </div>
    `;

    const closeBtn = card.querySelector(".toast-card-close");
    closeBtn.addEventListener("click", () => window.dismissToastCard(card));

    const progressBar = card.querySelector(".toast-card-progress-bar");
    progressBar.addEventListener("animationend", () => window.dismissToastCard(card));

    container.appendChild(card);
};

// Tự động kích hoạt đóng khi hết giờ cho các Card render từ Server TempData
document.querySelectorAll(".toast-card-box").forEach(function (card) {
    const progressBar = card.querySelector(".toast-card-progress-bar");
    if (progressBar) {
        progressBar.addEventListener("animationend", function () {
            window.dismissToastCard(card);
        });
    }
});
```

---

## 3. THIẾT KẾ CHI TIẾT CHO KHUNG CHAT REAL-TIME & PHÍM TẮT ENTER

### 3.1. Trình Xử lý Phím tắt Enter trên `#comment-content-input`
Trong `ticket-realtime.js`:
```javascript
if (commentInput && commentForm) {
    commentInput.addEventListener("keydown", function (e) {
        // Nhấn Enter mà không giữ Shift
        if (e.key === "Enter" && !e.shiftKey) {
            // Không gửi nếu đang gõ bộ gõ IME Telex tiếng Việt
            if (e.isComposing) return;

            e.preventDefault();

            const text = commentInput.value.trim();
            if (text.length === 0) return;

            if (typeof commentForm.requestSubmit === "function") {
                commentForm.requestSubmit();
            } else {
                commentForm.dispatchEvent(new Event("submit", { cancelable: true }));
            }
        }
    });
}
```

### 3.2. Cơ chế Deduplication & Optimistic UI Update
1. **Trong `appendCommentToView(comment)`**:
   ```javascript
   function appendCommentToView(comment) {
       if (!commentList) return;

       // 1. Kiểm tra chống trùng lặp theo ID
       if (comment.id && commentList.querySelector(`[data-comment-id="${comment.id}"]`)) {
           return; // Đã tồn tại trong danh sách, bỏ qua
       }

       const isMe = comment.authorUserId === currentUserId;
       const bubbleClass = comment.isInternal 
           ? "chat-bubble-internal" 
           : (isMe ? "chat-bubble-me" : "chat-bubble-other");
       const alignClass = isMe ? "ms-auto text-end" : "me-auto text-start";
       const metaAlign = isMe ? "justify-content-end" : "justify-content-start";

       const internalBadge = comment.isInternal 
           ? '<span class="badge bg-warning text-dark me-1">Nội bộ IT</span>' 
           : '';

       const roleBadge = comment.authorRole 
           ? `<span class="badge bg-secondary-subtle ms-1">${escapeHtml(comment.authorRole)}</span>`
           : '';

       const commentHtml = `
           <div class="mb-3 d-flex flex-column ${alignClass}" style="max-width: 80%;" data-comment-id="${comment.id || ''}">
               <div class="d-flex align-items-center mb-1 small text-muted ${metaAlign}">
                   ${internalBadge}
                   <strong>${escapeHtml(comment.authorName)}</strong>
                   ${roleBadge}
                   <span class="ms-2">${escapeHtml(comment.formattedTime || 'Vừa xong')}</span>
               </div>
               <div class="p-3 rounded-3 ${bubbleClass} text-start">
                   ${escapeHtml(comment.content).replace(/\n/g, '<br/>')}
               </div>
           </div>`;

       const emptyMsg = commentList.querySelector(".empty-comment-placeholder");
       if (emptyMsg) emptyMsg.remove();

       commentList.insertAdjacentHTML("beforeend", commentHtml);
   }
   ```
2. **Trong AJAX Form submit**:
   ```javascript
   if (response.ok) {
       const resData = await response.json().catch(() => ({}));
       if (resData.comment) {
           appendCommentToView(resData.comment);
           scrollToBottom();
       }
       commentInput.value = "";
       if (isInternalCheckbox) isInternalCheckbox.checked = false;
   } else {
       const errData = await response.json().catch(() => ({}));
       window.showToastCard({
           type: "error",
           title: "Không thể gửi bình luận",
           message: errData.message || "Đã có lỗi xảy ra khi gửi bình luận."
       });
   }
   ```
3. **Thay thế `alert(...)` và SVG icon trong `ticket-realtime.js`**:
   - Khi mạng lỗi:
     ```javascript
     window.showToastCard({
         type: "error",
         title: "Lỗi kết nối máy chủ",
         message: "Không thể kết nối tới máy chủ khi gửi phản hồi."
     });
     ```
   - Thay `<i class="bi bi-lock-fill"></i>` bằng:
     ```javascript
     commentLockNotice.innerHTML = `
         <div class="alert alert-warning py-2 mb-0 border-0 rounded-3 small">
             <svg class="bi me-1" width="16" height="16"><use xlink:href="#lock-fill"/></svg>
             Yêu cầu hiện ở trạng thái <strong>${data.newStatusLabel}</strong>. Chức năng gửi bình luận đã bị khóa.
         </div>`;
     ```

### 3.3. Cải tiến `signalr-notification.js`
1. Thay `showBootstrapToast`:
   ```javascript
   connection.on("ReceiveNotification", function (notification) {
       console.log("[SIGNALR] Thông báo mới:", notification);
       window.showToastCard({
           type: "info",
           title: "Thông báo mới",
           message: notification.message,
           link: notification.link,
           duration: 6000
       });
       prependNotificationItem(notification);
   });
   ```
2. Thay `alert` trong `ForceLogout`:
   ```javascript
   connection.on("ForceLogout", function (reason) {
       window.showToastCard({
           type: "error",
           title: "Phiên đăng nhập chấm dứt",
           message: reason,
           duration: 8000
       });
       setTimeout(function () {
           window.location.href = "/Auth/Login?error=" + encodeURIComponent(reason);
       }, 1500);
   });
   ```

---

## 4. BẢN THIẾT KẾ SỬA ĐỔI BACKEND CONTROLLER & DOMAIN EVENTS

Trong `User/TicketController.cs`, `StaffTicketController.cs`, và `LeaderTicketController.cs`:

```csharp
var commentResult = Comment.Create(ticket.Id, CurrentUserId.Value, content, isInternal);
if (commentResult.IsFailure)
{
    if (IsAjaxRequest) return BadRequest(new { message = commentResult.Error.Description });
    TempData["ErrorMessage"] = commentResult.Error.Description;
    return RedirectToAction(nameof(Details), new { id });
}

await _dbContext.Comments.AddAsync(commentResult.Value);
await _dbContext.SaveChangesAsync(); // Sinh ra commentResult.Value.Id từ SQLite

// Kích hoạt Domain Event: Phân phối Notification, SystemLog và SignalR WebSocket
var commentEvent = new CommentAddedEvent(
    commentResult.Value.Id,
    ticket.Id,
    CurrentUserId.Value,
    content,
    isInternal,
    commentResult.Value.CreatedAt
);
ticket.AddDomainEvent(commentEvent);
await _dbContext.SaveChangesAsync(); // Kích hoạt DispatchInTransaction & DispatchPostCommit

var author = await _dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == CurrentUserId.Value);
var commentDto = new CommentDto
{
    Id = commentResult.Value.Id,
    TicketId = ticket.Id,
    AuthorUserId = CurrentUserId.Value,
    AuthorName = author?.FullName ?? User.Identity?.Name ?? "Người dùng",
    AuthorRole = author?.Role.ToString() ?? User.FindFirst(ClaimTypes.Role)?.Value ?? "User",
    Content = content,
    IsInternal = isInternal,
    CreatedAt = commentResult.Value.CreatedAt
};

if (IsAjaxRequest)
    return Ok(new { success = true, message = "Bình luận đã gửi.", comment = commentDto });
```

---

## 5. KẾ HOẠCH NÂNG CẤP & BỔ SUNG KIỂM THỬ (TEST PLAN)

1. **Bảo tồn Bộ Kiểm thử Hiện có (336 tests)**:
   - Toàn bộ 10 kịch bản trong `Table72VerificationTests` tiếp tục pass 100%. Cần lưu ý kịch bản `TC-08` xác minh `AddComment` trên ticket `Resolved` bị chặn: cấu trúc `if (!ticket.CanComment())` được đặt ở đầu hàm trước khi gọi `Comment.Create`, do đó `TC-08` vẫn đảm bảo không có comment nào được lưu và không có sự kiện nào được kích hoạt.
2. **Bổ sung Unit/Integration Tests cho `AddComment` Domain Event**:
   - Viết bài kiểm thử `AddComment_ValidTicket_PublishesCommentAddedEventAndDispatchesSignalR`:
     - Tạo ticket ở trạng thái `InProgress`.
     - Gọi `AddComment` qua controller.
     - Xác minh `Comments` có thêm bản ghi mới.
     - Xác minh thông báo được thêm vào bảng `Notifications` cho người nhận.
     - Xác minh `SystemLogs` ghi nhận hành động thêm bình luận.
   - Viết bài kiểm thử `AddComment_InternalNote_NeverDispatchesNotificationToUser`:
     - Staff thêm ghi chú `isInternal = true`.
     - Xác minh không có bản ghi nào trong `Notifications` dành cho `CreatorId`.

---

## 6. KẾT LUẬN & KIẾN NGHỊ CHO BƯỚC THI CÔNG

1. Toàn bộ các nguyên nhân gốc rễ (Root Causes) của lỗi phím Enter, mất tin nhắn SignalR, và tồn dư `alert(...)` / `<i class="bi ...">` đã được xác định chính xác theo từng số dòng cụ thể.
2. Bản thiết kế Floating Card Box hoàn toàn độc lập, nhẹ (Zero external library, Zero CDN), tương thích 100% với hệ màu Enterprise Neutral Light & Carbon Dark của IT-TMS, có đầy đủ hiệu ứng xếp chồng, SVG sprite, và thanh đếm ngược tự dừng khi rê chuột.
3. Bộ kiểm thử 336 test cases hiện có là điểm tựa vững chắc để kiểm soát hồi quy (regression) trong quá trình triển khai.
