# Dispatch Record

## 2026-10-25T13:19:47Z
From: parent (4d5573d8-b0ff-4e1c-8244-b64b90d96c3d)
To: Project Orchestrator (orchestrator_2)

Mission & Key Requirements:
1. R1: Khắc phục triệt để lỗi Khung Chat Real-time SignalR & Phím tắt Enter (`User`, `Staff`, `Leader` ticket details):
   - Phím Enter (không giữ Shift) tự động gửi bình luận ngay lập tức; Shift+Enter xuống dòng.
   - Backend `AddComment` ở cả 3 phân hệ phát sự kiện `CommentAddedEvent` để đẩy `ReceiveComment` qua SignalR và `ReceiveNotification` chuông báo, kèm ghi log hệ thống.
   - Tin nhắn hiển thị tức thì trên màn hình người gửi, đẩy thời gian thực sang người nhận, cơ chế chống trùng lặp (deduplication) theo `data-comment-id`.
   - Ghi chú nội bộ (`IsInternal = true`) chỉ hiển thị cho Staff, Leader, Admin, không phát sang User.
2. R2: Rà soát & Đồng bộ triệt để Bảng màu Enterprise Neutral Light & Carbon Dark trên toàn bộ 23 Views (5 Areas):
   - Xóa bỏ toàn bộ `text-primary`, `text-danger` trên tiêu đề `h1`-`h6`, mã `#ID` dùng monospace trung tính.
   - Mức ưu tiên Low/Medium dùng pill trung tính (`bg-secondary-subtle`), chỉ High/Critical/Quá hạn dùng màu cảnh báo.
   - Thẻ KPI trên 4 Dashboard đồng nhất tông màu trung tính.
   - Đảm bảo độ tương phản WCAG AA >= 4.5:1 ở cả Light và Dark mode.
3. R3: Thiết kế lại Hệ thống Thông báo Thành công / Thất bại / Cảnh báo / SignalR dạng Floating Card Box Nhiều Tầng:
   - Floating Card Box neo cố định góc trên bên phải (top-right), xếp chồng mượt mà.
   - Khung thẻ nổi (shadow-float), khối Icon SVG, Dòng Tiêu đề phân loại + Nội dung chi tiết + Thanh tiến trình đếm ngược 5s (hover pause).
   - Loại bỏ toàn bộ `alert()` mặc định và `<i class="bi ...">` không có font, thay bằng inline SVG.
   - Xử lý thông báo lỗi trang Login dạng floating card không xô lệch bố cục 2 cột.
4. R4: Bảo toàn 100% Nghiệp vụ & Bộ Kiểm thử Tự động:
   - Đảm bảo toàn bộ 20 Use Cases và >= 336 bài test xUnit (bao gồm 10 kịch bản Table 7.2) đều PASS 100%.
   - Bổ sung/cập nhật unit tests cho luồng phát `CommentAddedEvent`.
   - Kiểm thử thực tế trên trình duyệt xác nhận các tính năng chat và thông báo hoạt động hoàn hảo.
