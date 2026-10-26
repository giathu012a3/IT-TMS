## 2026-10-26T09:27:45Z
BẠN LÀ: Worker Diagrams R3 (teamwork_preview_worker).
Thư mục làm việc của bạn: `c:\IT_System\.agents\teamwork\worker_diagrams_r3`
Project root: `c:\IT_System`

MANDATORY FIRST STEP: Đọc `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, `c:\IT_System\.agents\teamwork\orchestrator_4\PROJECT.md`, `c:\IT_System\.agents\teamwork\explorer_survey_1\codebase_inventory.md`, và `c:\IT_System\.agents\teamwork\explorer_survey_3\tooling_survey.md`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

QUYỀN SỞ HỮU TỆP TIN ĐỘC QUYỀN (WRITE OWNERSHIP):
Bạn sở hữu độc quyền việc tạo mã nguồn PlantUML (`.puml`) và xuất file ảnh render (`.png`) cho 13 sơ đồ kỹ thuật cũ cần hiện đại hóa (R3) tại thư mục:
`c:\IT_System\docs\diagrams\`
Cụ thể gồm:
1. **5 Sơ đồ Use Case Chương 2**:
   - `Hinh2_1_UseCaseTongThe.puml` & `.png`: Use Case tổng thể toàn hệ thống IT-TMS (4 Actors: User, Leader, Staff, Admin).
   - `Hinh2_2_UseCaseUser.puml` & `.png`: Phân hệ Người dùng (Gửi yêu cầu, theo dõi, chat, nghiệm thu).
   - `Hinh2_3_UseCaseLeader.puml` & `.png`: Phân hệ Trưởng nhóm (Duyệt, phân công, điều chuyển, từ chối, giám sát SLA).
   - `Hinh2_4_UseCaseStaff.puml` & `.png`: Phân hệ Kỹ thuật viên (Tiếp nhận, xử lý, yêu cầu hỗ trợ, giải quyết).
   - `Hinh2_5_UseCaseAdmin.puml` & `.png`: Phân hệ Quản trị & Bảo mật (Quản lý User, phân quyền, cấu hình hệ thống).
2. **8 Sơ đồ Sequence Diagrams Chương 5**:
   - `Hinh5_4_Sequence_UC01_Login.puml` & `.png`: UC01 Đăng nhập & Xác thực (`AuthController.Login`, `PasswordHasherService`, JWT/Cookie).
   - `Hinh5_5_Sequence_UC03_CreateTicket.puml` & `.png`: UC03 Tạo yêu cầu hỗ trợ SLA (`TicketController.Create`, `NotificationObserver`, `TicketHub`).
   - `Hinh5_7_Sequence_UC07_ProcessTicket.puml` & `.png`: UC07 Tiếp nhận và chuyển trạng thái InProgress (`StaffController.StartProcessing`, State Pattern).
   - `Hinh5_8_Sequence_UC06_ChatRealtime.puml` & `.png`: UC06 Trao đổi trực tiếp thời gian thực (`TicketController.AddComment`, `TicketHub.ReceiveComment`).
   - `Hinh5_9_Sequence_UC10_RejectTicket.puml` & `.png`: UC10 Từ chối yêu cầu (`LeaderController.Reject`, kiểm tra trạng thái khác InProgress).
   - `Hinh5_10_Sequence_UC04_19_Feedback.puml` & `.png`: UC04 & UC19 Đánh giá nghiệm thu và đóng Ticket (`TicketController.Feedback`, `ResolvedState` -> `ClosedState`).
   - `Hinh5_11_Sequence_UC13_CreateUser.puml` & `.png`: UC13 Tạo người dùng mới (`AdminController.CreateUser`, `[AuthorizeRoles("Admin")]`).
   - `Hinh5_12_Sequence_UC16_ConfigSLA.puml` & `.png`: UC16 Cấu hình SLA hệ thống (`AdminController.ConfigSLA` hoặc `LeaderController`).

YÊU CẦU KỸ THUẬT RENDER:
- Sử dụng Kroki API (`https://kroki.io/plantuml/png` hoặc script Python từ `explorer_survey_3\tooling_survey.md`).
- Chuẩn thẩm mỹ cao: `skinparam dpi 300`, nền sáng sang trọng (#FFFFFF / #F8F9FA), đường nét sắc sảo, font Segoe UI / Arial, chuẩn hóa đúng 100% C# .NET 9 controller/method/SignalR, xóa sạch toàn bộ tàn dư Python/Flask cũ.
- Đảm bảo độ phân giải cao từ 2000px trở lên, chữ đọc sắc nét, chuẩn UML 2.5.

KẾT QUẢ ĐẦU RA:
- 13 file `.puml` và 13 file `.png` tại `c:\IT_System\docs\diagrams\`.
- Viết nhật ký tiến độ: `c:\IT_System\.agents\teamwork\worker_diagrams_r3\progress.md`.
- Viết báo cáo chi tiết: `c:\IT_System\.agents\teamwork\worker_diagrams_r3\report.md`.
- Viết báo cáo bàn giao: `c:\IT_System\.agents\teamwork\worker_diagrams_r3\handoff.md`.
- Gửi tin nhắn hoàn thành cho Orchestrator (Recipient: 3cc6747f-b9cb-4239-907a-6853ec500168).
KHÔNG sửa file docx. Chỉ tạo diagram source và render image.
