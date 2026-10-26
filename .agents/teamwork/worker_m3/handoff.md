# BÁO CÁO BÀN GIAO: WORKER M3 (FRONTEND REALTIME CHAT & ENTER SHORTCUT)

## 1. Observation (Quan sát thực tế)
- **Tập tin thuộc quyền sở hữu độc quyền**: `src/ItTms.Web/wwwroot/js/ticket-realtime.js`.
- **Quan sát trước khi sửa đổi**:
  - Không có sự kiện bắt phím `Enter` trên `#comment-content-input` (chỉ có sự kiện `submit` trên form). Người dùng nhấn Enter chỉ xuống dòng, không gửi được bình luận.
  - Sau khi gửi form qua AJAX thành công (`response.ok`), mã chỉ thực hiện `commentInput.value = "";` và thụ động chờ SignalR, không hiển thị bình luận ngay trên màn hình người gửi.
  - Hàm `appendCommentToView(comment)` sinh ra HTML thẻ `div` không có thuộc tính `data-comment-id`, đồng thời không có bước kiểm tra phần tử đã tồn tại trong DOM trước khi chèn, dẫn đến nguy cơ nhân đôi (duplicate) bong bóng chat khi cả response AJAX và sự kiện SignalR `ReceiveComment` cùng trả về.
  - Tồn dư 2 lệnh `alert(...)` chặn luồng trình duyệt tại dòng 104 và 108 khi gửi thất bại hoặc mất kết nối mạng.
  - Sử dụng thẻ `<i class="bi bi-lock-fill me-1"></i>` tại dòng 65 trong thông báo khóa ô bình luận khi ticket ở trạng thái không cho phép.
- **Quan sát sau khi sửa đổi**:
  - Sự kiện `keydown` đã được gắn trên `#comment-content-input` với điều kiện `e.key === "Enter" && !e.shiftKey && !e.isComposing`.
  - Khi AJAX trả về `response.ok`, ô nhập được xóa trắng và hàm `appendCommentToView(resData.comment)` được gọi ngay lập tức, cuộn xuống đáy `scrollToBottom()`.
  - Trong `appendCommentToView(comment)`:
    - Kiểm tra `if (commentId && document.querySelector(`[data-comment-id="${commentId}"]`)) return;` trước khi chèn vào DOM.
    - Gắn `data-comment-id="${commentId || ''}"` trên phần tử bao ngoài bong bóng chat.
    - Chuẩn hóa xử lý cả định dạng camelCase lẫn PascalCase từ server DTO.
  - 100% lệnh `alert(...)` đã bị loại bỏ hoàn toàn khỏi file, thay bằng `window.showToastCard('danger', 'Lỗi', errorMsg)` (có fallback an toàn `console.error`).
  - Thẻ `<i class="bi bi-lock-fill me-1"></i>` được thay thế bằng SVG sprite `<svg class="bi me-1" width="16" height="16"><use href="#lock-fill"></use></svg>`.
- **Kết quả kiểm thử & biên dịch**:
  - Lệnh `dotnet build c:\IT_System\ItTms.sln` trả về: `Build succeeded. 0 Warning(s), 0 Error(s).`
  - Lệnh `dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --no-build` trả về: `Passed! - Failed: 0, Passed: 342, Skipped: 0, Total: 342`.

---

## 2. Logic Chain (Chuỗi lập luận & thiết kế)
1. **Phím tắt Enter & Tương thích bộ gõ tiếng Việt**:
   - Nhấn `Enter` mà không giữ `Shift` (`!e.shiftKey`) là quy ước chuẩn cho thao tác gửi tin nhắn trong ứng dụng chat hiện đại.
   - Thêm điều kiện `!e.isComposing` để đảm bảo khi người dùng đang gõ tiếng Việt bằng bộ gõ IME Telex/VNI (đang gõ dấu hoặc ghép từ), phím Enter xác nhận từ của bộ gõ sẽ không kích hoạt gửi form nhầm.
   - Khi thỏa mãn, gọi `e.preventDefault()` để chặn chèn ký tự xuống dòng `\n`, sau đó kích hoạt `commentForm.requestSubmit()` (hoặc dispatch event `submit` nếu trình duyệt không hỗ trợ `requestSubmit`).
   - Nhấn `Shift + Enter` không bị chặn, cho phép người dùng xuống dòng bình thường.
2. **Hiển thị tức thì (Optimistic/Instant Display)**:
   - Thay vì chờ đợi WebSocket round-trip từ SignalR, khi AJAX POST phản hồi thành công với payload `{ success: true, message: "...", comment: commentDto }`, client lập tức xóa trắng ô textarea và gọi `appendCommentToView(resData.comment)` để tin nhắn hiện ngay lập tức trên màn hình người gửi.
3. **Chống trùng lặp (Deduplication)**:
   - Cả response AJAX và sự kiện SignalR `ReceiveComment` đều gọi qua hàm duy nhất `appendCommentToView(comment)`.
   - Mỗi comment được gán `data-comment-id="${commentId}"`.
   - Trước khi chèn, hàm kiểm tra `document.querySelector(`[data-comment-id="${commentId}"]`)`. Nếu phần tử với ID đó đã tồn tại trong DOM (do AJAX hiển thị trước hoặc do SignalR tới trước), hàm lập tức dừng lại (`return`), bảo đảm không bao giờ bị nhân đôi bong bóng chat.
4. **Triệt tiêu `alert(...)` & Chuẩn hóa Floating Card Box**:
   - `alert(...)` là dialog nguyên thủy của trình duyệt gây nghẽn UI luồng chính và làm giảm trải nghiệm người dùng. Việc thay thế bằng `window.showToastCard('danger', 'Lỗi', message)` đồng bộ hóa hoàn toàn với hệ thống Floating Card Box của M2.
5. **Chuẩn hóa SVG Sprite**:
   - Thẻ `<i>` phụ thuộc vào font icon webfont, dễ gặp lỗi hiển thị ô vuông hoặc chậm tải font. Thay thế bằng `<svg class="bi me-1" width="16" height="16"><use href="#lock-fill"></use></svg>` bảo đảm hiển thị sắc nét, tức thì và độc lập với font ngoài.

---

## 3. Caveats (Lưu ý & Phạm vi)
- **Phạm vi sở hữu**: Worker M3 chỉ chỉnh sửa duy nhất `src/ItTms.Web/wwwroot/js/ticket-realtime.js`. Không can thiệp vào Controller, Razor Views hay các file JS khác.
- **Hạ tầng hỗ trợ**: `window.showToastCard` được cung cấp bởi `signalr-notification.js` (hoặc `site.js`). Trong trường hợp hàm này chưa sẵn sàng, mã nguồn đã có fallback ghi nhật ký lỗi an toàn qua `console.error` để không gây crash ứng dụng.

---

## 4. Conclusion (Kết luận)
Toàn bộ 5 yêu cầu kỹ thuật của Worker M3 đã được hoàn thiện đạt chuẩn 100%:
1. Đã có shortcut Enter gửi bình luận (Shift + Enter xuống dòng, tương thích IME Telex/VNI).
2. Đã có hiển thị tức thì bên người gửi ngay khi nhận phản hồi AJAX commentDto.
3. Đã có cơ chế chống trùng lặp theo `data-comment-id` ngăn chặn triệt để lặp bong bóng chat.
4. Đã triệt tiêu 100% `alert(...)`, thay bằng `window.showToastCard`.
5. Đã triệt tiêu 100% thẻ `<i class="bi ...">`, thay bằng SVG sprite `#lock-fill`.
6. Biên dịch thành công 0 warnings, 0 errors; toàn bộ 342 bài kiểm thử vượt qua 100%.

---

## 5. Verification Method (Phương pháp kiểm chứng độc lập)
1. **Kiểm tra biên dịch & mã nguồn**:
   ```powershell
   dotnet build c:\IT_System\ItTms.sln
   ```
   Xác nhận: `0 Warning(s)`, `0 Error(s)`.
2. **Kiểm tra bộ unit & integration tests**:
   ```powershell
   dotnet test c:\IT_System\tests\ItTms.Tests\ItTms.Tests.csproj --no-build
   ```
   Xác nhận: Toàn bộ 342 test cases PASS.
3. **Quét kiểm tra không còn alert hoặc thẻ i icon**:
   - Tìm `alert(` trong `src/ItTms.Web/wwwroot/js/ticket-realtime.js`: 0 kết quả.
   - Tìm `<i ` trong `src/ItTms.Web/wwwroot/js/ticket-realtime.js`: 0 kết quả.
   - Kiểm tra thuộc tính `data-comment-id` trong `appendCommentToView`: có mặt đầy đủ.
