# BRIEFING — 2026-10-26T09:54:00Z

## Mission
Thực hiện chèn mới 6 sơ đồ kỹ thuật thiếu và thay thế 13 sơ đồ mờ cũ bằng ảnh PNG 300 DPI chuẩn vào cả 2 tài liệu Word (`BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`), đảm bảo 100% tính toàn vẹn tài liệu (149 headings, 136 TOC, 55 tables, 41/41 figures, 0 lỗi schema Word COM).

## 🔒 My Identity
- Archetype: preview_worker
- Roles: implementer, qa, specialist
- Working directory: c:\IT_System\.agents\teamwork\worker_insert_images
- Original parent: 3cc6747f-b9cb-4239-907a-6853ec500168
- Milestone: Milestone 4 - Safe Insertion & Replacement of 19 High-Res Diagrams into Word Documents

## 🔒 Key Constraints
- Áp dụng `#Argon-Omega` cognitive kernel: Không sycophancy, giải pháp cấp độ production, zero fluff.
- Ngôn ngữ: Tiếng Việt làm chủ đạo, giữ nguyên các thuật ngữ kỹ thuật và mã code/định danh.
- Quyền sở hữu độc quyền: 2 file Word `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx` và `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`.
- BẮT BUỘC backup vào `c:\IT_System\backups\backup_before_image_insertion\` trước khi can thiệp.
- Chèn mới 6 sơ đồ thiếu (3.1, 5.1, 5.2, 5.3, 5.6, 6.1) ngay phía trên caption paragraph tương ứng, căn giữa, kích thước chuẩn (~6.0 inch).
- Thay thế 13 sơ đồ mờ/cũ (2.1-2.5, 5.4, 5.5, 5.7-5.12) bằng sơ đồ 300 DPI tương ứng tại `c:\IT_System\docs\diagrams\`.
- Giữ nguyên 22 sơ đồ giao diện Chương 4 (Hình 4.1 - 4.22) không gây xáo trộn.
- Đảm bảo bất biến cấu trúc: 149 headings, 136 mục TOC, 55 bảng, 41/41 hình ảnh.
- Xác minh độc lập bằng Python OpenXML parser và Microsoft Word 16.0 COM Automation (0 lỗi XML corruption, mở file trơn tru).

## Current Parent
- Conversation ID: 3cc6747f-b9cb-4239-907a-6853ec500168
- Updated: 2026-10-26T09:54:00Z

## Task Summary
- **What to build**: Hoàn thành xuất sắc 100% việc chèn 6 ảnh mới và thay thế 13 ảnh cũ vào cả 2 file Word; bảo toàn 149 headings, 136 TOC, 55 bảng; 0 lỗi schema.
- **Success criteria**: Cả 2 file docx chứa đủ 41 hình ảnh; 149 heading; 136 TOC items; 55 tables; Word 16.0 COM mở trơn tru không báo lỗi; báo cáo đầy đủ.
- **Interface contracts**: `c:\IT_System\.agents\teamwork\orchestrator_4\PROJECT.md`.
- **Code layout**: Ảnh tại `c:\IT_System\docs\diagrams\`, scripts tại worker workspace, reports trong `.agents\teamwork\worker_insert_images\`.

## Change Tracker
- **Files modified**:
  - `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx`: Đã chèn 6 ảnh mới, thay thế 13 ảnh cũ 300 DPI, bảo toàn 41 hình.
  - `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`: Đã chèn 6 ảnh mới, thay thế 13 ảnh cũ 300 DPI, bảo toàn 41 hình.
- **Build status**: PASS (149 headings, 136 TOC, 55 tables, 41/41 figures, Word 16.0 COM pass).
- **Pending issues**: None. Ready for Milestone 5.

## Quality Status
- **Build/test result**: PASS 100% trên cả Python OpenXML audit và Microsoft Word 16.0 COM Automation.
- **Lint status**: 0 synthetic namespaces (`ns\d+` = 0).
- **Tests added/modified**: `verify_full_integrity.py`, `verify_word_com_production.ps1`.

## Loaded Skills
- N/A

## Key Decisions Made
- Bảo toàn nguyên vẹn thuộc tính `pStyle` (`Heading 2` và `Heading 3`) của 13 container paragraph chứa ảnh cũ để duy trì đúng 149 Headings và 136 TOC.
- Chèn 6 ảnh thiếu với paragraph style `Normal` để không làm lệch số lượng Heading.
- Ánh xạ chính xác sơ đồ Staff vào Hình 2.3 và Leader vào Hình 2.4 theo đúng nội dung phân hệ thực tế.
- Nhúng `Hinh5_11_Sequence_UC15_ToggleUser.png` và `Hinh5_12_Sequence_UC11_DashboardLeader.png` khớp 100% caption Word.

## Artifact Index
- `c:\IT_System\.agents\teamwork\worker_insert_images\DISPATCH.md` — Phân công nhiệm vụ
- `c:\IT_System\.agents\teamwork\worker_insert_images\BRIEFING.md` — Bộ nhớ làm việc
- `c:\IT_System\.agents\teamwork\worker_insert_images\progress.md` — Tiến độ thực hiện
- `c:\IT_System\.agents\teamwork\worker_insert_images\report.md` — Báo cáo kỹ thuật chi tiết
- `c:\IT_System\.agents\teamwork\worker_insert_images\handoff.md` — Báo cáo bàn giao
- `c:\IT_System\.agents\teamwork\worker_insert_images\apply_images_production.py` — Script chèn ảnh chính thức
- `c:\IT_System\.agents\teamwork\worker_insert_images\verify_full_integrity.py` — Script kiểm định OpenXML
- `c:\IT_System\.agents\teamwork\worker_insert_images\verify_word_com_production.ps1` — Script kiểm định Word COM
