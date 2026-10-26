# BRIEFING — 2026-10-25T13:51:08Z

## Mission
Thiết kế và triển khai Multi-tier Floating Card Box Notification System cho IT-TMS, thay thế toàn bộ flat toast/alert inline bằng floating card box đa tầng, hỗ trợ thanh tiến trình 5s, hover-pause, SVG icons, đồng bộ Light/Dark mode WCAG AA.

## 🔒 My Identity
- Archetype: implementer / specialist
- Roles: implementer, qa, specialist
- Working directory: c:\IT_System\.agents\teamwork\worker_m2
- Original parent: ff0e71b2-5cba-4663-bd07-29f7639ba776
- Milestone: worker_m2 (Multi-tier Floating Card Box Notification System)

## 🔒 Key Constraints
- Exclusive file ownership:
  - `src/ItTms.Web/wwwroot/css/site.css`
  - `src/ItTms.Web/Views/Shared/_ToastAlerts.cshtml`
  - `src/ItTms.Web/Views/Shared/_SvgIcons.cshtml`
  - `src/ItTms.Web/Views/Shared/_Layout.cshtml`
  - `src/ItTms.Web/Areas/Auth/Views/Auth/Login.cshtml`
  - `src/ItTms.Web/wwwroot/js/signalr-notification.js`
- DO NOT edit any other files.
- Integrity mandate: No cheating, no facade implementations, genuine logic.
- WCAG AA contrast ratio >= 4.5:1 across Light & Dark modes.
- SignalR & TempData notifications rendered as multi-layer floating card boxes with SVG symbols (no `<i class="bi ...">`).
- Zero build errors on `dotnet build c:\IT_System\ItTms.sln`.
- Language: Tiếng Việt.

## Current Parent
- Conversation ID: ff0e71b2-5cba-4663-bd07-29f7639ba776
- Updated: 2026-10-25T13:51:08Z

## Task Summary
- **What to build**: Floating Card Box Notification System (CSS stack & cards with 5s progress animation and hover-pause, SVG symbol support, server-side TempData rendering in `_ToastAlerts.cshtml`, client-side SignalR & JS toast trigger via `window.showToastCard`, clean Login without inline shift, layout injection check).
- **Success criteria**: 0 build errors, clean responsive multi-tier floating card notifications with pause-on-hover, dark mode WCAG AA, SVG icons.
- **Interface contracts**: `_ToastAlerts.cshtml`, `window.showToastCard(...)` in `signalr-notification.js`.

## Key Decisions Made
- `_SvgIcons.cshtml`: Bổ sung symbol `#info-circle-fill`, đảm bảo đủ 6 symbols chuẩn SVG.
- `site.css`: Định nghĩa `.floating-notification-stack`, `.floating-card-box`, `.floating-card-progress` chạy 0% -> 100% 5s, hover-pause và `.is-paused` đóng băng tiến trình, biến thể ngữ nghĩa 4px start-border, contrast WCAG AA >= 4.5:1.
- `_ToastAlerts.cshtml`: Render container `#toast-container` cố định và hiển thị `TempData` (`SuccessMessage`, `ErrorMessage`, `WarningMessage`, `InfoMessage`), `ModelState` errors và query error thành multi-tier card box.
- `Login.cshtml`: Xóa bỏ alert lỗi nội dòng làm lệch layout 2 cột; chuyển sang hiển thị qua floating card box.
- `_Layout.cshtml`: Thêm `data-authenticated` vào `<body>`, xóa container trùng lặp, đưa `_ToastAlerts` ra ngoài `<main>`, tải `signalr-notification.js` toàn cục.
- `signalr-notification.js`: Cung cấp `window.showToastCard` và `window.dismissToastCard`, thay thế 1-line flat toast, thay thế `alert()` trong `ForceLogout` bằng card box đếm ngược 10s và điều hướng trễ 2.5s.

## Artifact Index
- `handoff.md` — Final handoff report
- `progress.md` — Realtime progress tracker
- `DISPATCH.md` — Log nhiệm vụ và trao đổi từ Orchestrator

## Change Tracker
- **Files modified**:
  - `src/ItTms.Web/Views/Shared/_SvgIcons.cshtml`: Thêm symbol `#info-circle-fill`.
  - `src/ItTms.Web/wwwroot/css/site.css`: Thêm styles cho floating notification stack & cards.
  - `src/ItTms.Web/Views/Shared/_ToastAlerts.cshtml`: Tái cấu trúc thành multi-tier floating card box.
  - `src/ItTms.Web/Areas/Auth/Views/Auth/Login.cshtml`: Xóa alert nội dòng gây shift layout.
  - `src/ItTms.Web/Views/Shared/_Layout.cshtml`: Dọn dẹp toast container trùng lặp, thêm `data-authenticated`.
  - `src/ItTms.Web/wwwroot/js/signalr-notification.js`: Cung cấp `window.showToastCard`, xóa `alert()`, dùng SVG sprite.
- **Build status**: `dotnet build c:\IT_System\ItTms.sln` PASSED (0 Warnings, 0 Errors).
- **Pending issues**: None

## Quality Status
- **Build/test result**: Pass (0 errors).
- **Lint status**: Clean
- **Tests added/modified**: Co-located e2e & layout verification.

## Loaded Skills
- N/A
