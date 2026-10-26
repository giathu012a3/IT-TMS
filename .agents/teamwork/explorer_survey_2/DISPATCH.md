## 2026-10-26T09:04:03Z
BẠN LÀ: Explorer Survey 2 (teamwork_preview_explorer).
Thư mục làm việc của bạn: `c:\IT_System\.agents\teamwork\explorer_survey_2`
Project root: `c:\IT_System`

MANDATORY FIRST STEP: Đọc `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, `c:\IT_System\PROJECT_MEMORY.md`, và `c:\IT_System\AGENTS.md`.

NHIỆM VỤ CỦA BẠN:
Khảo sát toàn diện 2 tài liệu Word:
- File 1: `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx`
- File 2: `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`

Yêu cầu cụ thể:
1. Trích xuất và kiểm đếm danh sách tiêu đề (headings): Xác nhận chính xác 149 tiêu đề trong tài liệu, kiểm tra tính toàn vẹn mục lục (TOC) và cấu trúc phân cấp Heading 1, 2, 3...
2. Khảo sát chi tiết Bảng phân công công việc Phụ lục 1 (Bảng PL.1) trong cả 2 tài liệu Word: nội dung hiện tại là gì, vị trí ở đâu, đang chứa những nội dung nào chưa đúng chuẩn hoặc có dính devops/unit test cần loại bỏ.
3. Lập danh mục toàn bộ 41 hình ảnh trong tài liệu Word:
   - Số hiệu hình, tên caption đầy đủ (Hình 1.1 đến 6.1 hoặc tương ứng).
   - Vị trí trong tài liệu (Chương/Mục nào).
   - Tình trạng hiện tại: hình đang có sẵn, hình bị thiếu (R2: Hình 3.1, 5.1, 5.2, 5.3, 5.6, 6.1), hay hình cũ mờ/chưa đồng bộ .NET 9 (R3: Hình 2.1-2.5, 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12).
   - Kích thước, định dạng và cách nhúng hình trong tài liệu Word.

KẾT QUẢ ĐẦU RA:
- Ghi nhận tiến độ liveness trong `c:\IT_System\.agents\teamwork\explorer_survey_2\progress.md`.
- Xuất báo cáo chi tiết vào `c:\IT_System\.agents\teamwork\explorer_survey_2\document_inventory.md`.
- Viết `c:\IT_System\.agents\teamwork\explorer_survey_2\handoff.md` theo chuẩn Handoff Protocol.
- Gửi tin nhắn hoàn thành cho Orchestrator (Recipient: 3cc6747f-b9cb-4239-907a-6853ec500168).
KHÔNG sửa file docx. Bạn là read-only explorer. Dùng script python nếu cần đọc docx để khảo sát chính xác.
