## 2026-10-25T14:01:34Z
You are Worker M3 (worker_m3): Frontend Realtime Chat & Enter Shortcut.
Your working directory is: c:\IT_System\.agents\teamwork\worker_m3
You MUST use send_message to communicate all results, reports, and updates back to your caller (the Project Orchestrator).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY FIRST STEP:
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md under header `## 2026-10-25T13:17:29Z` and c:\IT_System\.agents\teamwork\explorer_survey_3\analysis.md.

YOUR EXCLUSIVE FILE OWNERSHIP:
- `src/ItTms.Web/wwwroot/js/ticket-realtime.js`
DO NOT edit any other file.

YOUR TASK SPECIFICATION:
1. Shortcut phím Enter:
   - Trong `ticket-realtime.js`, tìm `#comment-content-input`.
   - Gắn sự kiện `keydown`: khi người dùng nhấn `Enter` (không giữ `Shift` và `!e.isComposing` để tương thích bộ gõ tiếng Việt Telex/VNI), ngăn mặc định (`e.preventDefault()`) và kích hoạt gửi form (`commentForm.requestSubmit()` hoặc gọi hàm gửi Ajax). Nhấn `Shift + Enter` xuống dòng bình thường.
2. Hiển thị tức thì bên người gửi (Optimistic/Instant Display):
   - Khi gửi Ajax thành công (Backend trả về `{ success: true, message: "...", comment: commentDto }`), ngay lập tức xóa trắng textarea và gọi `appendCommentToView(data.comment)` để tin nhắn hiện tức thì trên màn hình người gửi mà không cần chờ SignalR hay reload trang.
3. Chống trùng lặp (Deduplication theo `data-comment-id`):
   - Mọi phần tử comment được sinh ra qua `appendCommentToView(comment)` phải có thuộc tính `data-comment-id="${comment.id}"`.
   - Trước khi chèn comment mới vào DOM (cả từ response Ajax lẫn từ sự kiện `ReceiveComment` của SignalR), kiểm tra xem `document.querySelector(`[data-comment-id="${comment.id}"]`)` đã tồn tại chưa. Nếu đã có thì bỏ qua (return), tuyệt đối không nhân đôi bong bóng chat trên màn hình người gửi.
4. Triệt tiêu `alert(...)` mặc định:
   - Tìm và loại bỏ 100% lệnh `alert(...)` (dòng 104, 108 hoặc bất kỳ vị trí nào khác trong file), thay thế bằng hàm `window.showToastCard('danger', 'Lỗi', message)` hoặc `window.showToastCard('warning', 'Cảnh báo', message)` đã được triển khai trong M2.
5. Triệt tiêu thẻ `<i class="bi ...">`:
   - Thay thế thẻ `<i class="bi bi-lock-fill me-1"></i>` (tại dòng 65 hoặc bất kỳ chỗ nào) bằng SVG sprite `<svg class="bi me-1" width="16" height="16"><use href="#lock-fill"></use></svg>`.
6. Biên dịch xác nhận:
   - Chạy `dotnet build c:\IT_System\ItTms.sln` xác nhận 0 Warning(s), 0 Error(s).

DELIVERABLE:
Viết báo cáo bàn giao đầy đủ vào `c:\IT_System\.agents\teamwork\worker_m3\handoff.md` và gửi tin nhắn hoàn tất về Orchestrator.
