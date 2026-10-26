# Quality Audit & E2E Verification Plan — orchestrator_3

## Objective
Thực hiện kiểm tra chất lượng toàn diện (Quality Audit) và nghiệm thu đầu-cuối (E2E Verification) của hệ thống IT-TMS (.NET 9 C# 13) theo yêu cầu tại `ORIGINAL_REQUEST.md` (mục `## 2026-10-25T14:44:33Z`).

## Architecture & Work Breakdown Structure (WBS)

### Stream 1 (R3): Solution Build & Automated Test Suite Verification
- Mục tiêu: Kiểm tra nghiệm thu `ItTms.sln` biên dịch 0 cảnh báo, 0 lỗi; 342 automated tests PASS (bao gồm 10/10 kịch bản Table 7.2).
- Nhiệm vụ:
  1. Survey Explorer kiểm tra trạng thái build và test hiện tại.
  2. Worker sửa bất kỳ lỗi cảnh báo, lỗi biên dịch, hoặc test failure (nếu có).
  3. Reviewers & Challengers xác minh độc lập.

### Stream 2 (R2): Report Integrity & WordprocessingML Synchronization
- Mục tiêu: Kiểm tra nghiệm thu `BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`.
- Tiêu chí:
  - Mở được bằng Microsoft Word không lỗi XML namespace hoặc schema.
  - 0 tiền tố tổng hợp `ns0:`/`ns1:`.
  - Bảo toàn 100% 373 headings và hệ thống phân cấp Mục lục (Table of Contents).
  - Tài liệu hóa chuẩn xác .NET 9, Clean Architecture, EF Core 9, SignalR, và 10/10 kịch bản đạt của Bảng 7.2.
- Nhiệm vụ:
  1. Survey Explorer kiểm tra cấu trúc docx, headings, XML namespace.
  2. Worker điều chỉnh/sửa nếu phát hiện vi phạm.
  3. Reviewers & Challengers kiểm tra tính tương thích Word.

### Stream 3 (R1): UI/UX & Frontend Verification across 5 Areas
- Mục tiêu: Kiểm tra và hoàn thiện giao diện 5 Areas theo Taste Skill v2 (`achromatic-ledger` / `true-black-flat`).
- Tiêu chí:
  - Quick-fill buttons ở trang Login có độ tương phản rõ ràng, dễ đọc.
  - Không có footer thừa trong `_Layout.cshtml`.
  - Flash alert container gọn gàng (max-width: 460px) có auto-dismiss.
  - Bảng quản lý người dùng Admin sắp xếp theo ID tăng dần (ID ascending) với các nút thao tác căn thẳng hàng đồng đều.
  - Các thẻ KPI được cấu trúc sạch sẽ qua `<section class="kpis">`.
  - Không có chữ tương phản thấp khó đọc hoặc vỡ style ở Dark Mode trên cả 5 Areas.
- Nhiệm vụ:
  1. Survey Explorer rà soát toàn bộ 23 views và CSS/JS.
  2. Worker thực thi các điều chỉnh UI/UX cần thiết.
  3. Reviewers & Challengers kiểm tra độ tương phản và layout.

## Verification Gate Criteria (Strict AND)
1. Build 0 warnings, 0 errors; 342/342 tests pass.
2. Word document: XML namespace chuẩn, 373 headings nguyên vẹn, TOC hierarchy bảo toàn, nội dung .NET 9 đầy đủ.
3. Frontend: Dark mode contrast >= 4.5:1, flash alert max-width 460px, Admin table sorted ID asc & buttons aligned, KPI `<section class="kpis">`, no redundant footer.
4. Reviewers: Tất cả APPROVE.
5. Challengers: Kiểm thử thực tế xác nhận PASS.
6. Forensic Auditor: CLEAN (Không có giả mạo, không có vi phạm tính toàn vẹn).
