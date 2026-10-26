# BRIEFING — 2026-10-26T09:20:00Z

## Mission
Khảo sát công cụ, môi trường thực thi (Python, Graphviz, PlantUML, Mermaid CLI, v.v.), tài nguyên sơ đồ hiện có và đề xuất quy trình kỹ thuật render sơ đồ chất lượng cao (300 DPI) & nhúng an toàn vào file DOCX.

## 🔒 My Identity
- Archetype: explorer
- Roles: investigator, synthesizer
- Working directory: c:\IT_System\.agents\teamwork\explorer_survey_3
- Original parent: 3cc6747f-b9cb-4239-907a-6853ec500168
- Milestone: survey_tooling_and_diagram_assets

## 🔒 Key Constraints
- Read-only investigation — do NOT implement
- Không chỉnh sửa file mã nguồn dự án hay tài liệu docx
- Chỉ ghi tài liệu và báo cáo vào c:\IT_System\.agents\teamwork\explorer_survey_3\
- Ngôn ngữ giao tiếp: Tiếng Việt, giữ nguyên thuật ngữ kỹ thuật và mã định danh
- Áp dụng nguyên lý Argon-Omega: chính xác, thực chứng, zero-fluff

## Current Parent
- Conversation ID: 3cc6747f-b9cb-4239-907a-6853ec500168
- Updated: 2026-10-26T09:20:00Z

## Investigation State
- **Explored paths**: Python environment (`pip list`), system CLI tools (`where.exe java`, `dot`, `node`, `npm`, `npx`, `chrome`), Word COM automation, docx internal XML & media structures, Kroki API, sample PlantUML generation.
- **Key findings**:
  1. Python 3.12.4 có sẵn `python-docx` 1.2.0, `pillow` 12.3.0, `lxml` 6.1.3, `selenium` 4.51.0, `requests` 2.34.2.
  2. Hệ thống chưa có Java và Graphviz binary `dot.exe`, nhưng Kroki API hoạt động xuất sắc (<1s) với PlantUML sinh ảnh PNG 300 DPI lên tới 4096x2168px; Headless Chrome + Selenium làm phương án dự phòng.
  3. Repo có 0 file source sơ đồ; 22 screenshots Ch.4 đã hoàn thiện; tài liệu Word có 41 hình vẽ gồm: 6 hình thiếu hoàn toàn (Hình 3.1, 5.1, 5.2, 5.3, 5.6, 6.1) và 13 hình cũ mờ (Hình 2.1-2.5, 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12).
  4. Đã thực nghiệm và chứng minh thuật toán chèn ảnh mới (`insert_paragraph_before`) và thay thế ảnh cũ (`remove <w:r>`) bằng python-docx đạt 0 lỗi XML schema corruption qua kiểm định thực tế bằng Microsoft Word 16.0 COM Automation.
- **Unexplored areas**: Không còn (Hoàn thành 100% nhiệm vụ).

## Key Decisions Made
- Chọn PlantUML qua Kroki API kết hợp Pillow nhúng DPI metadata làm pipeline render sơ đồ chính; Chrome Headless Selenium làm fallback.
- Viết sẵn mã nguồn PlantUML mẫu cho 6 sơ đồ mới trong báo cáo khảo sát.

## Artifact Index
- `c:\IT_System\.agents\teamwork\explorer_survey_3\DISPATCH.md` — Tin nhắn chỉ đạo ban đầu
- `c:\IT_System\.agents\teamwork\explorer_survey_3\BRIEFING.md` — Bộ nhớ hoạt vụ
- `c:\IT_System\.agents\teamwork\explorer_survey_3\progress.md` — Tiến độ liveness
- `c:\IT_System\.agents\teamwork\explorer_survey_3\tooling_survey.md` — Báo cáo khảo sát công cụ & tài nguyên chi tiết
- `c:\IT_System\.agents\teamwork\explorer_survey_3\handoff.md` — Báo cáo bàn giao chuẩn Handoff Protocol
- `c:\IT_System\.agents\teamwork\explorer_survey_3\samples\sample_fig_3_1_erd.png` — Ảnh mẫu ERD 8 bảng 300 DPI (2868x1973 px)
