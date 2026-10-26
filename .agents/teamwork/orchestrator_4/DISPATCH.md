## 2026-10-26T09:02:46Z
Bạn là Project Orchestrator (teamwork_preview_orchestrator) cho dự án IT-TMS (.NET 9 ASP.NET Core MVC).

### Thông tin môi trường và thư mục làm việc:
- Working directory của bạn: `c:\IT_System\.agents\teamwork\orchestrator_4`
- Project root: `c:\IT_System`
- Yêu cầu gốc của người dùng được lưu tại: `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md` (mục `## 2026-10-26T09:01:24Z`).
- Single-source-of-truth: `c:\IT_System\PROJECT_MEMORY.md` và `c:\IT_System\AGENTS.md`.

### Nhiệm vụ trọng tâm:
Thực hiện đồng bộ hóa toàn diện tài liệu báo cáo Word của hệ thống IT-TMS (.NET 9 ASP.NET Core MVC):
1. **R1: Chuẩn hóa Bảng phân công công việc (Bảng PL.1)** ở cả 2 tài liệu Word (`BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`) theo 4 phân hệ dọc độc lập (Vertical Slices: 1 Người = 1 Phân hệ = 1 Controller = 1 Mẫu thiết kế GoF):
   - **Nguyễn Gia Thụ (25810041)**: Kỹ thuật viên (Staff), `StaffController.cs`, `Views/Staff/`, State Pattern, State Machine Hình 5.2, Class Diagram State Pattern Hình 6.1, Sequence UC07 Hình 5.7.
   - **Nguyễn Mạnh Tiến (25810043)**: Trưởng nhóm (Leader), `LeaderController.cs`, `Views/Leader/`, Factory Method / Flyweight Pattern, BPMN Hình 5.1, Sequence UC09 Hình 5.6, Sequence UC10 Hình 5.9.
   - **Hồ Thanh Tình (25810044)**: Người dùng (User), `TicketController.cs`, `Views/Ticket/`, Observer Pattern (SignalR WebSocket `TicketHub` & SystemLog), Sequence UC03 Hình 5.5, Sequence UC06 Hình 5.8, Sequence UC04/19 Hình 5.10.
   - **Trần Thanh Danh (25810009)**: Quản trị & Bảo mật (Admin & Auth), `AdminController.cs`, `AuthController.cs`, `Views/Admin/`, `Views/Auth/`, Decorator Pattern (`[AuthorizeRoles]`), Singleton Pattern (`PasswordHasherService`), ERD 8 bảng Hình 3.1, Class Diagram tổng thể Hình 5.3, Use Case tổng thể Hình 2.1-2.5.
   - Loại bỏ triệt để các nội dung ngoài lề môn học (unit test / devops).
   - Bảo toàn 100% (149/149) tiêu đề và cấu trúc mục lục (TOC).

2. **R2: Thiết kế mới và chèn toàn bộ các sơ đồ kỹ thuật đang thiếu** (Hình 3.1 ERD 8 bảng, Hình 5.1 BPMN quy trình xử lý, Hình 5.2 State Machine 7 trạng thái, Hình 5.3 Class Diagram tổng quát, Hình 5.6 Sequence UC09 Phân công, Hình 6.1 Class Diagram State Pattern) với độ nét cao, nền sáng sang trọng, bám sát 100% C# .NET 9.

3. **R3: Hiện đại hóa và đồng bộ hóa các sơ đồ cũ sang chuẩn .NET 9** (Hình 2.1 - 2.5 Use Case diagrams, Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12 Sequence diagrams) thành sơ đồ UML mới, sắc nét, đúng class/controller/method/SignalR, không còn bất kỳ hình mờ/tàn dư cũ, bảo đảm toàn bộ 41 hình ảnh trong tài liệu đồng bộ hoàn hảo.

### Quy trình điều phối:
- Tạo kế hoạch `plan.md` và thường xuyên cập nhật tiến độ vào `progress.md` trong working directory của bạn.
- Triệu tập các subagent chuyên trách (explorers, workers, reviewers, challengers) để khảo sát, sinh sơ đồ chất lượng cao, cập nhật file docx an toàn và kiểm tra tính toàn vẹn (validate XML, kiểm tra 0 lỗi corrupt, đếm heading, verify hình ảnh).
- Khi hoàn thành tất cả các mục tiêu và kiểm tra chất lượng chặt chẽ, gửi báo cáo hoàn thành cho Sentinel để tiến hành Victory Audit độc lập.
