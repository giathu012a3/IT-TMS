## 2026-10-26T09:41:54Z

BẠN LÀ: Worker Insert Images (teamwork_preview_worker).
Thư mục làm việc của bạn: `c:\IT_System\.agents\teamwork\worker_insert_images`
Project root: `c:\IT_System`

MANDATORY FIRST STEP: Đọc `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, `c:\IT_System\.agents\teamwork\orchestrator_4\PROJECT.md`, `c:\IT_System\.agents\teamwork\worker_table_pl1\handoff.md`, `c:\IT_System\.agents\teamwork\worker_diagrams_r2\handoff.md`, `c:\IT_System\.agents\teamwork\worker_diagrams_r3\handoff.md`, `c:\IT_System\.agents\teamwork\explorer_survey_2\document_inventory.md`, và `c:\IT_System\.agents\teamwork\explorer_survey_3\tooling_survey.md`.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

QUYỀN SỞ HỮU TỆP TIN ĐỘC QUYỀN (WRITE OWNERSHIP):
Bạn sở hữu độc quyền việc đọc, tạo backup và cập nhật 2 tệp tin Word:
- `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx`
- `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`
Thư mục backup: `c:\IT_System\backups\`

NGUỒN HÌNH ẢNH 300 DPI CÓ SẴN TẠI `c:\IT_System\docs\diagrams\`:
1. 6 Sơ đồ kỹ thuật mới (R2):
   - `Hinh3_1_ERD_8Bang.png` (Hình 3.1)
   - `Hinh5_1_BPMN_TicketLifecycle.png` (Hình 5.1)
   - `Hinh5_2_StateMachine_Ticket.png` (Hình 5.2)
   - `Hinh5_3_ClassDiagram_MVC.png` (Hình 5.3)
   - `Hinh5_6_Sequence_UC09_PhanCong.png` (Hình 5.6)
   - `Hinh6_1_StatePattern_ClassDiagram.png` (Hình 6.1)
2. 13 Sơ đồ kỹ thuật hiện đại hóa (R3):
   - `Hinh2_1_UseCaseTongThe.png` (Hình 2.1)
   - `Hinh2_2_UseCaseUser.png` (Hình 2.2)
   - `Hinh2_3_UseCaseLeader.png` (Hình 2.3)
   - `Hinh2_4_UseCaseStaff.png` (Hình 2.4)
   - `Hinh2_5_UseCaseAdmin.png` (Hình 2.5)
   - `Hinh5_4_Sequence_UC01_Login.png` (Hình 5.4)
   - `Hinh5_5_Sequence_UC03_CreateTicket.png` (Hình 5.5)
   - `Hinh5_7_Sequence_UC07_ProcessTicket.png` (Hình 5.7)
   - `Hinh5_8_Sequence_UC06_ChatRealtime.png` (Hình 5.8)
   - `Hinh5_9_Sequence_UC10_RejectTicket.png` (Hình 5.9)
   - `Hinh5_10_Sequence_UC04_19_Feedback.png` (Hình 5.10)
   - `Hinh5_11_Sequence_UC13_CreateUser.png` (Hình 5.11)
   - `Hinh5_12_Sequence_UC16_ConfigSLA.png` (Hình 5.12)
3. 22 Sơ đồ giao diện live Chương 4 (Hình 4.1 đến 4.22): Giữ nguyên hiện trạng, không thay đổi.

NHIỆM VỤ CỤ THỂ (MILESTONE 4):
1. **Tạo bản sao lưu an toàn**: Copy 2 file docx vào `c:\IT_System\backups\backup_before_image_insertion\`.
2. **Thay thế và chèn hình ảnh an toàn vào cả 2 tài liệu Word**:
   - Sử dụng kỹ thuật python-docx an toàn (đã được Explorer 3 chứng minh và kiểm thử qua Word COM):
     - Chèn mới 6 hình thiếu (Hình 3.1, 5.1, 5.2, 5.3, 5.6, 6.1) ngay phía trên đoạn văn chứa caption tương ứng, căn giữa (center), chiều rộng phù hợp (Inches(6.0) đến Inches(6.2) tùy tỉ lệ ảnh), giữ nguyên paragraph caption bên dưới.
     - Thay thế 13 hình cũ/mờ (Hình 2.1-2.5, Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12) bằng ảnh PNG 300 DPI mới tương ứng.
     - Đảm bảo 22 hình giao diện Chương 4 không bị xáo trộn.
3. **Kiểm tra tính toàn vẹn (Verification)**:
   - Kiểm tra bằng script Python và OpenXML: Cả 2 tài liệu phải có chính xác 149 tiêu đề (`Heading *`), 136 mục TOC, 55 bảng, và 41/41 hình ảnh hiện diện đầy đủ, đúng vị trí caption.
   - Mở thử file bằng Microsoft Word 16.0 COM Automation để kiểm tra: file mở bình thường, 0 lỗi schema / XML corruption, bố cục trang đẹp mắt.

KẾT QUẢ ĐẦU RA:
- Viết nhật ký tiến độ: `c:\IT_System\.agents\teamwork\worker_insert_images\progress.md`.
- Viết báo cáo kỹ thuật chi tiết: `c:\IT_System\.agents\teamwork\worker_insert_images\report.md`.
- Viết báo cáo bàn giao: `c:\IT_System\.agents\teamwork\worker_insert_images\handoff.md`.
- Gửi tin nhắn hoàn thành cho Orchestrator (Recipient: 3cc6747f-b9cb-4239-907a-6853ec500168).
