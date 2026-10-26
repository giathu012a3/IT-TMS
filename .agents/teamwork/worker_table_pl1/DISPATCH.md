## 2026-10-26T09:27:45Z
BẠN LÀ: Worker Table PL1 (teamwork_preview_worker).
Thư mục làm việc của bạn: `c:\IT_System\.agents\teamwork\worker_table_pl1`
Project root: `c:\IT_System`

MANDATORY FIRST STEP: Đọc `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, `c:\IT_System\.agents\teamwork\orchestrator_4\PROJECT.md`, `c:\IT_System\.agents\teamwork\explorer_survey_2\handoff.md`, và `c:\IT_System\.agents\teamwork\explorer_survey_2\document_inventory.md`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

QUYỀN SỞ HỮU TỆP TIN ĐỘC QUYỀN (WRITE OWNERSHIP):
Bạn sở hữu độc quyền việc đọc, tạo backup và cập nhật 2 tệp tin Word:
- `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx`
- `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`
- Thư mục backup: `c:\IT_System\backups\`

NHIỆM VỤ CỤ THỂ (MILESTONE 1):
1. **Tạo bản sao lưu an toàn**: Copy 2 file docx vào `c:\IT_System\backups\backup_before_pl1_update\`.
2. **Chuẩn hóa Bảng phân công công việc Phụ lục 1 (Bảng PL.1)** trong cả 2 tài liệu Word theo đúng ma trận 4 Vertical Slices độc lập:
   - **Nguyễn Gia Thụ (25810041)**: Kỹ thuật viên (Staff), `StaffController.cs`, `Views/Staff/`, State Pattern, State Machine Hình 5.2, Class Diagram State Pattern Hình 6.1, Sequence UC07 Hình 5.7.
   - **Nguyễn Mạnh Tiến (25810043)**: Trưởng nhóm (Leader), `LeaderController.cs`, `Views/Leader/`, Factory Method / Flyweight Pattern, BPMN Hình 5.1, Sequence UC09 Hình 5.6, Sequence UC10 Hình 5.9.
   - **Hồ Thanh Tình (25810044)**: Người dùng (User), `TicketController.cs`, `Views/Ticket/`, Observer Pattern (SignalR WebSocket `TicketHub` & SystemLog), Sequence UC03 Hình 5.5, Sequence UC06 Hình 5.8, Sequence UC04/19 Hình 5.10.
   - **Trần Thanh Danh (25810009)**: Quản trị & Bảo mật (Admin & Auth), `AdminController.cs`, `AuthController.cs`, `Views/Admin/`, `Views/Auth/`, Decorator Pattern (`[AuthorizeRoles]`), Singleton Pattern (`PasswordHasherService`), ERD 8 bảng Hình 3.1, Class Diagram tổng thể Hình 5.3, Use Case tổng thể Hình 2.1-2.5.
   - **LOẠI BỎ TRIỆT ĐỂ** các nội dung ngoài lề môn học (unit test / devops) khỏi Bảng PL.1.
   - Đảm bảo giữ nguyên phong cách bảng biểu chuyên nghiệp, căn lề đẹp mắt, tỷ lệ đóng góp 100% cho mỗi thành viên.
3. **Dọn dẹp bảng tạm thừa ở Doc2**: Xóa bỏ 7 bảng tạm `[CHÈN HÌNH]` thừa (Table #42, #43, #44, #45, #46, #47, #48 đã được Explorer 2 phát hiện) để cấu trúc bảng ở Doc2 đồng nhất với Doc1 (55 bảng).
4. **Kiểm tra tính toàn vẹn (Verification)**:
   - Kiểm tra bằng script Python và OpenXML: Cả 2 tài liệu phải có chính xác 149 tiêu đề (`Heading *`), mục lục TOC 136 mục không bị lệch hoặc mất format.
   - Mở thử file bằng Microsoft Word 16.0 COM Automation để kiểm tra: file mở bình thường, 0 lỗi XML schema corruption.

KẾT QUẢ ĐẦU RA:
- Viết nhật ký tiến độ: `c:\IT_System\.agents\teamwork\worker_table_pl1\progress.md`.
- Viết báo cáo chi tiết: `c:\IT_System\.agents\teamwork\worker_table_pl1\report.md`.
- Viết báo cáo bàn giao: `c:\IT_System\.agents\teamwork\worker_table_pl1\handoff.md` theo chuẩn Handoff Protocol.
- Gửi tin nhắn hoàn thành cho Orchestrator (Recipient: 3cc6747f-b9cb-4239-907a-6853ec500168).
