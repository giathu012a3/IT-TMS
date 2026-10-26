## 2026-10-26T09:04:03Z
BẠN LÀ: Explorer Survey 1 (teamwork_preview_explorer).
Thư mục làm việc của bạn: `c:\IT_System\.agents\teamwork\explorer_survey_1`
Project root: `c:\IT_System`

MANDATORY FIRST STEP: Đọc `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, `c:\IT_System\PROJECT_MEMORY.md`, và `c:\IT_System\AGENTS.md`.

NHIỆM VỤ CỦA BẠN:
Khảo sát toàn diện mã nguồn C# .NET 9 ASP.NET Core MVC tại `c:\IT_System`:
1. Liệt kê chính xác 8 bảng cơ sở dữ liệu / Entity trong DbContext (`c:\IT_System\Models\` hoặc `c:\IT_System\Data\`). Ghi rõ các trường chính, khóa chính, khóa ngoại, quan hệ giữa các bảng.
2. Khảo sát 4 phân hệ dọc (Vertical Slices) tương ứng với 4 thành viên:
   - Phân hệ Kỹ thuật viên (Staff - Nguyễn Gia Thụ): `StaffController.cs`, `Views/Staff/`, implementation của State Pattern, 7 trạng thái của Ticket (State Machine: New, Assigned, InProgress, PendingCustomer, Resolved, Closed, Cancelled...), các phương thức chuyển trạng thái.
   - Phân hệ Trưởng nhóm (Leader - Nguyễn Mạnh Tiến): `LeaderController.cs`, `Views/Leader/`, Factory Method / Flyweight Pattern, quy trình xử lý ticket, phân công kỹ thuật viên, phê duyệt ticket.
   - Phân hệ Người dùng (User - Hồ Thanh Tình): `TicketController.cs`, `Views/Ticket/`, Observer Pattern kết hợp SignalR WebSocket `TicketHub.cs` (`Hubs/TicketHub.cs`), SystemLog, tạo ticket, xem tiến độ, bình luận thời gian thực.
   - Phân hệ Quản trị & Bảo mật (Admin & Auth - Trần Thanh Danh): `AdminController.cs`, `AuthController.cs`, `Views/Admin/`, `Views/Auth/`, Decorator Pattern (`[AuthorizeRoles]`), Singleton Pattern (`PasswordHasherService`), quản lý người dùng, phân quyền, đăng nhập, bảo mật.
3. Liệt kê đầy đủ các Controller actions, routes, methods, SignalR events (`ReceiveTicketUpdate`, `ReceiveComment`, v.v.) và các service liên quan.

KẾT QUẢ ĐẦU RA:
- Ghi nhận tiến độ liveness trong `c:\IT_System\.agents\teamwork\explorer_survey_1\progress.md`.
- Xuất báo cáo chi tiết vào `c:\IT_System\.agents\teamwork\explorer_survey_1\codebase_inventory.md`.
- Viết `c:\IT_System\.agents\teamwork\explorer_survey_1\handoff.md` theo chuẩn Handoff Protocol (Observation, Logic Chain, Caveats, Conclusion, Verification Method).
- Gửi tin nhắn hoàn thành cho Orchestrator (Recipient: 3cc6747f-b9cb-4239-907a-6853ec500168).
KHÔNG sửa code hay file dự án. Bạn là read-only explorer.
