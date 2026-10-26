# BRIEFING — 2026-10-25T15:09:00Z

## Mission
Thực hiện tinh chỉnh quy tắc CSS cho Dark Mode tại `src/ItTms.Web/wwwroot/css/site.css` (bổ sung `.table > tbody > tr.table-light > td`), dọn dẹp tiến trình nền khóa tệp nếu có, biên dịch sạch sẽ Solution (0 Warnings, 0 Errors) và chạy nghiệm thu 100% (342/342) unit/integration/E2E tests của hệ thống IT-TMS (.NET 9).

## 🔒 My Identity
- Archetype: worker
- Roles: implementer, qa, specialist
- Working directory: c:\IT_System\.agents\teamwork\worker_polish_ui
- Original parent: 6b5c148e-154e-40de-9b25-e016c63addcf
- Milestone: UI Polish & Test Verification Final Acceptance

## 🔒 Key Constraints
- File duy nhất được sở hữu chỉnh sửa mã: `src/ItTms.Web/wwwroot/css/site.css`.
- Áp dụng nguyên tắc Minimal Change, không refactor ngoài phạm vi được giao.
- Tuân thủ chuẩn Enterprise Neutral Light & Carbon Dark của Taste Skill v2.
- Kiểm tra tiến trình `ItTms.Web` đang chạy chiếm file DLL và dừng an toàn trước khi build.
- `dotnet build c:\IT_System\ItTms.sln` phải đạt 0 Warning(s), 0 Error(s).
- `dotnet test c:\IT_System\ItTms.sln` phải đạt 342/342 tests PASS (100%), bao gồm trọn vẹn 10/10 kịch bản `Table72VerificationTests`.
- Integrity Mandate: Không giả mạo dữ liệu hay tạo facade tests.

## Current Parent
- Conversation ID: 6b5c148e-154e-40de-9b25-e016c63addcf
- Updated: 2026-10-25T15:09:00Z

## Task Summary
- **What to build**: Thêm quy tắc CSS trong `src/ItTms.Web/wwwroot/css/site.css` cho `.table > tbody > tr.table-light > td` với `background-color: var(--subtle) !important;` và `color: var(--text) !important;`, dừng tiến trình `ItTms.Web` khóa file, build 0 warnings/errors, test 342/342 pass.
- **Success criteria**: CSS được bổ sung đúng dòng 588-591, build 0 cảnh báo 0 lỗi, 342 tests pass 100%, handoff.md hoàn chỉnh.
- **Interface contracts**: PROJECT.md / SCOPE.md
- **Code layout**: Clean Architecture (.NET 9)

## Key Decisions Made
- Chèn quy tắc `.table > tbody > tr.table-light > td` ngay sau `.table > tbody > tr:last-child > td` trong mục `/* ---------- Tables ---------- */` của `src/ItTms.Web/wwwroot/css/site.css` để bảo đảm tính phân cấp và kế thừa logic CSS chuẩn.
- Dừng triệt để tiến trình `ItTms.Web` (PID 16668/4152) trước khi chạy build để tránh lỗi khóa nhị phân MSB3026.

## Artifact Index
- `c:\IT_System\.agents\teamwork\worker_polish_ui\DISPATCH.md` — Chỉ thị từ parent agent
- `c:\IT_System\.agents\teamwork\worker_polish_ui\BRIEFING.md` — Bộ nhớ hoạt động và ngữ cảnh agent
- `c:\IT_System\.agents\teamwork\worker_polish_ui\progress.md` — Nhật ký liveness và tiến độ
- `c:\IT_System\.agents\teamwork\worker_polish_ui\handoff.md` — Báo cáo nghiệm thu hoàn tất 5 phần

## Change Tracker
- **Files modified**: `src/ItTms.Web/wwwroot/css/site.css` (bổ sung dòng 588-591 quy tắc cho `.table > tbody > tr.table-light > td`)
- **Build status**: `Build succeeded. 0 Warning(s), 0 Error(s). Time Elapsed 00:00:08.35`
- **Pending issues**: Không có

## Quality Status
- **Build/test result**: PASS 100% (342/342 tests passed, bao gồm trọn vẹn 10/10 kịch bản Table 7.2)
- **Lint status**: 0 violations
- **Tests added/modified**: 0 (bảo toàn 342 bài kiểm thử hiện có)

## Loaded Skills
- **Source**: `c:\IT_System\.agents\skills\taste-skill\SKILL.md`
- **Local copy**: N/A
- **Core methodology**: Taste Skill v2 - achromatic-ledger / true-black-flat, WCAG AA >= 4.5:1 contrast, no visual noise.
