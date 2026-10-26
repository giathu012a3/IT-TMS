## 2026-10-26T09:04:03Z
BẠN LÀ: Explorer Survey 3 (teamwork_preview_explorer).
Thư mục làm việc của bạn: `c:\IT_System\.agents\teamwork\explorer_survey_3`
Project root: `c:\IT_System`

MANDATORY FIRST STEP: Đọc `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, `c:\IT_System\PROJECT_MEMORY.md`, và `c:\IT_System\AGENTS.md`.

NHIỆM VỤ CỦA BẠN:
Khảo sát công cụ, môi trường và tài nguyên để sinh và nhúng sơ đồ kỹ thuật chất lượng cao (300 DPI, nền sáng, thẩm mỹ cao):
1. Kiểm tra môi trường Python: phiên bản Python, các package đã cài (`python-docx`, `Pillow`, `matplotlib`, `graphviz`, v.v.).
2. Kiểm tra xem có sẵn công cụ đồ họa nào trong hệ thống (PlantUML jar, Graphviz dot.exe, mermaid CLI, v.v.).
3. Khảo sát các sơ đồ/assets hiện có trong dự án: xem thư mục `.agents/`, `docs/`, `wwwroot/`, hoặc các folder tài liệu xem có mã nguồn sơ đồ (.puml, .dot, .mmd) hoặc file ảnh nào đã được tạo trước đây không.
4. Đề xuất quy trình và chuẩn công nghệ tối ưu để:
   - Render 6 sơ đồ mới (Hình 3.1 ERD 8 bảng, Hình 5.1 BPMN, Hình 5.2 State Machine, Hình 5.3 Class Diagram, Hình 5.6 Sequence UC09, Hình 6.1 State Pattern Class Diagram).
   - Render và hiện đại hóa các sơ đồ Use Case (2.1-2.5) và Sequence (5.4-5.12).
   - Kỹ thuật thay thế/chèn ảnh an toàn vào tài liệu docx bằng python-docx (hoặc OOXML direct manipulation) đảm bảo 0 lỗi XML corrupt và giữ nguyên định dạng caption/paraph.

KẾT QUẢ ĐẦU RA:
- Ghi nhận tiến độ liveness trong `c:\IT_System\.agents\teamwork\explorer_survey_3\progress.md`.
- Xuất báo cáo chi tiết vào `c:\IT_System\.agents\teamwork\explorer_survey_3\tooling_survey.md`.
- Viết `c:\IT_System\.agents\teamwork\explorer_survey_3\handoff.md` theo chuẩn Handoff Protocol.
- Gửi tin nhắn hoàn thành cho Orchestrator (Recipient: 3cc6747f-b9cb-4239-907a-6853ec500168).
KHÔNG sửa code hay docx. Bạn là read-only explorer.
