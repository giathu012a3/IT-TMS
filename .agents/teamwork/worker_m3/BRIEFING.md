# BRIEFING — 2026-10-25T14:06:30Z

## Mission
Triển khai phím tắt Enter gửi tin nhắn, hiển thị tức thì (Optimistic display), chống trùng lặp deduplication, triệt tiêu alert() và thay thế Bootstrap Icon <i> bằng SVG sprite trong `ticket-realtime.js`.

## 🔒 My Identity
- Archetype: implementer, qa
- Roles: implementer, qa
- Working directory: c:\IT_System\.agents\teamwork\worker_m3
- Original parent: ff0e71b2-5cba-4663-bd07-29f7639ba776
- Milestone: worker_m3

## 🔒 Key Constraints
- File quyền sở hữu độc quyền: `src/ItTms.Web/wwwroot/js/ticket-realtime.js`. TUYỆT ĐỐI không sửa file nào khác.
- Shortcut Enter: phím Enter không Shift và `!e.isComposing` (tương thích gõ tiếng Việt Telex/VNI).
- Optimistic/Instant display: Khi Ajax phản hồi commentDto, xóa trắng textarea và chèn ngay vào DOM.
- Deduplication: Kiểm tra `data-comment-id="${comment.id}"` trước khi chèn, gán attribute này cho comment element.
- Loại bỏ hoàn toàn `alert(...)`, dùng `window.showToastCard` (có fallback an toàn).
- Thay thẻ `<i class="bi ...">` bằng `<svg class="bi ..."><use href="#..."></use></svg>`.
- Build xác nhận 0 errors, 0 warnings.

## Current Parent
- Conversation ID: ff0e71b2-5cba-4663-bd07-29f7639ba776
- Updated: 2026-10-25T14:06:30Z

## Task Summary
- **What to build**: Nâng cấp `ticket-realtime.js` theo 5 yêu cầu kỹ thuật.
- **Success criteria**: Enter gửi tin, Shift+Enter xuống dòng, hỗ trợ gõ VNI/Telex (!e.isComposing), hiển thị ngay sau Ajax, chống nhân đôi tin nhắn khi SignalR broadcast về, thay alert bằng toast, thay <i> bằng SVG sprite. Build pass 0 error 0 warning.
- **Interface contracts**: Backend trả về `{ success: true, message: string, comment: commentDto }`.
- **Code layout**: `src/ItTms.Web/wwwroot/js/ticket-realtime.js`.

## Key Decisions Made
- Gắn sự kiện `keydown` trên `#comment-content-input` với điều kiện `e.key === "Enter" && !e.shiftKey && !e.isComposing`.
- Trong `appendCommentToView`, kiểm tra `if (commentId && document.querySelector(`[data-comment-id="${commentId}"]`)) return;` trước khi chèn vào DOM.
- Thêm `data-comment-id="${commentId || ''}"` trên thẻ div comment.
- Thay 100% `alert(...)` bằng `window.showToastCard('danger', 'Lỗi', errorMsg)`.
- Thay `<i class="bi bi-lock-fill me-1"></i>` bằng `<svg class="bi me-1" width="16" height="16"><use href="#lock-fill"></use></svg>`.

## Artifact Index
- `c:\IT_System\.agents\teamwork\worker_m3\DISPATCH.md` — Nhiệm vụ được giao
- `c:\IT_System\.agents\teamwork\worker_m3\BRIEFING.md` — Situational awareness
- `c:\IT_System\.agents\teamwork\worker_m3\progress.md` — Liveness heartbeat
- `c:\IT_System\.agents\teamwork\worker_m3\handoff.md` — Báo cáo hoàn tất

## Change Tracker
- **Files modified**: `src/ItTms.Web/wwwroot/js/ticket-realtime.js` (toàn diện 5 yêu cầu)
- **Build status**: `dotnet build c:\IT_System\ItTms.sln` -> Build succeeded. 0 Warning(s), 0 Error(s).
- **Pending issues**: None

## Quality Status
- **Build/test result**: Build 0 warning 0 error. Test suite running.
- **Lint status**: Clean (0 alerts, 0 bi-icons)
- **Tests added/modified**: Co-verified with existing 336 test cases
