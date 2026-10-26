## 2026-10-25T14:47:49Z
Sender: 6b5c148e-154e-40de-9b25-e016c63addcf
Content:
Bạn là Explorer 3 chuyên trách kiểm tra UI/UX & Frontend Verification across 5 Areas (Requirement R1).
Thư mục làm việc của bạn: c:\IT_System\.agents\teamwork\survey_explorer_ui_ux

Tài liệu yêu cầu gốc:
Đọc kỹ c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md (đặc biệt mục ## 2026-10-25T14:44:33Z) và c:\IT_System\PROJECT.md.

Nhiệm vụ:
Rà soát toàn bộ mã nguồn Razor Views (23 views) tại `src/ItTms.Web/Areas/` (Auth, Admin, Leader, Staff, User), `src/ItTms.Web/Views/Shared/`, CSS và JS theo tiêu chuẩn Taste Skill v2 (achromatic-ledger / true-black-flat):
1. Quick-fill buttons ở trang Login (`Areas/Auth/Views/Auth/Login.cshtml`): kiểm tra độ tương phản text/background, có dễ đọc không cả ở Light và Dark mode?
2. `Views/Shared/_Layout.cshtml`: kiểm tra xem có footer thừa (redundant footer) nào không?
3. Flash alert container (`_ToastAlerts.cshtml`, `site.css`, JS): kiểm tra xem có bị tràn rộng không, đã được giới hạn `max-width: 460px` kèm auto-dismiss chưa?
4. Admin User Management (`Areas/Admin/Views/User/Index.cshtml`): bảng người dùng đã được sắp xếp theo ID tăng dần (ID ascending) chưa? Các nút hành động (action buttons) có thẳng hàng đồng đều không hay bị răng cưa/xô lệch?
5. Các thẻ KPI trên các Dashboard: đã được cấu trúc sạch sẽ qua `<section class="kpis">` chưa?
6. Quét toàn bộ Dark Mode trên cả 5 Areas: có văn bản nào có độ tương phản thấp khó đọc hoặc vỡ style không?
7. Cập nhật `progress.md` định kỳ và xuất báo cáo kết quả chi tiết kèm danh sách các file cần điều chỉnh (nếu có) vào `handoff.md` trong thư mục làm việc của bạn.
8. Khi hoàn thành, gửi tin nhắn thông báo cho parent.
