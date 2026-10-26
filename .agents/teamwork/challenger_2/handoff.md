# Adversarial Verification Report — Challenger 2 (Frontend & Visual Robustness)

## 1. Observation

### Challenge 1: Forbidden `alert(` or `window.alert(` calls in `src/ItTms.Web/wwwroot/js/`
- Tool: `grep_search` with regex `\balert\s*\(` on `c:\IT_System\src\ItTms.Web\wwwroot\js`
  - Result: 0 occurrences found.
- Tool: `grep_search` with query `window.alert` on `c:\IT_System\src\ItTms.Web\wwwroot\js`
  - Result: 0 occurrences found.
- Tool: Case-insensitive search for `alert` in `c:\IT_System\src\ItTms.Web\wwwroot\js`:
  - `signalr-notification.js:78`: `card.setAttribute("role", "alert");` (ARIA accessibility attribute)
  - `sla-calculator.js:17, 22, 25, 28, 31, 43`: Bootstrap CSS utility classes `alert-primary`, `alert-danger`, etc. for SLA preview banner
  - `ticket-realtime.js:68`: `<div class="alert alert-warning py-2 mb-0">` (warning banner markup when ticket comment is locked)
  - `site.js:48-54`: Dismissal logic for `.flash-alert-container .alert` via `bootstrap.Alert.getOrCreateInstance`
  - **Verdict on C1**: Zero calls to browser `alert()` or `window.alert()`.

### Challenge 2: Forbidden `<i class="bi` font icons in `signalr-notification.js` and `ticket-realtime.js`
- Tool: `grep_search` with query `<i class="bi` on `c:\IT_System\src\ItTms.Web\wwwroot\js`:
  - Result: 0 occurrences found.
- Tool: `grep_search` with regex `<i\b` on `c:\IT_System\src\ItTms.Web\wwwroot\js`:
  - Result: 0 occurrences found.
- Code Inspection:
  - `signalr-notification.js:88`: `<svg class="bi" width="18" height="18"><use xlink:href="${iconSymbol}"/></svg>`
  - `signalr-notification.js:96`: `<svg class="bi" width="14" height="14"><use xlink:href="#x-circle-fill"/></svg>`
  - `ticket-realtime.js:69`: `<svg class="bi me-1" width="16" height="16"><use href="#lock-fill"></use></svg>`
  - `ticket-realtime.js:154`: `<svg class="bi me-1" width="16" height="16"><use xlink:href="#send-fill"/></svg>`
  - **Verdict on C2**: 100% SVG inline / SVG sprite usage (`#lock-fill`, `#send-fill`, `#check-circle-fill`, `#x-circle-fill`, `#exclamation-triangle-fill`, `#bell-fill`). Zero `<i class="bi` webfont icon occurrences.

### Challenge 3: Unwanted `text-primary` or `text-danger` on `h1`–`h6` or `#ID` in `src/ItTms.Web/Areas/`
- Tool: `grep_search` with regex `<h[1-6][^>]*text-(primary|danger)` on `c:\IT_System\src\ItTms.Web\Areas`:
  - Result: 0 occurrences found.
- Tool: `grep_search` with query `text-primary` on `c:\IT_System\src\ItTms.Web\Areas`:
  - Result: 0 occurrences found across all 23 views.
- Tool: `grep_search` with query `text-danger` on `c:\IT_System\src\ItTms.Web\Areas`:
  - Result: Exactly 20 occurrences, all on `<span asp-validation-for="..." class="text-danger small"></span>` for form validation errors in `Login.cshtml`, `ChangePassword.cshtml`, `Ticket/Create.cshtml`, `User/Create.cshtml`, `User/Edit.cshtml`.
  - Zero `text-danger` on headings, badges, or static card titles.
- Tool: `grep_search` with query `#@` on `c:\IT_System\src\ItTms.Web\Areas`:
  - Result: All table `#ID` columns use `<td class="ps-4 fw-bold font-monospace text-muted">#@ticket.Id</td>` (e.g. `LeaderDashboard/Index.cshtml:163`, `User/Dashboard/Index.cshtml:85`, `Staff/Dashboard/Index.cshtml:79, 137`, `StaffTicket/Assigned.cshtml:68`, `StaffTicket/History.cshtml:42`, `User/Ticket/Index.cshtml:80`, `Admin/User/Index.cshtml:96`, `Admin/Audit/Index.cshtml:94`).
  - Page headers use clean neutral `#@Model.Id: @Model.Title` inside `<h3 class="fw-bold mb-0">` without color classes.
  - **Verdict on C3**: Completely compliant with Enterprise Neutral Light & Carbon Dark palette rules.

### Challenge 4: Verify `Staff/Dashboard/Index.cshtml` priority colors
- File inspection: `c:\IT_System\src\ItTms.Web\Areas\Staff\Views\Dashboard\Index.cshtml`:
  - Line 82-84 (Urgent SLA table):
    ```csharp
    <span class="badge @(ticket.Priority == TicketPriority.Critical ? "bg-danger" : ticket.Priority == TicketPriority.High ? "bg-warning" : "bg-secondary-subtle")">
        @ticket.Priority
    </span>
    ```
  - Line 145-147 (Active tickets table):
    ```csharp
    <span class="badge @(ticket.Priority == TicketPriority.Critical ? "bg-danger" : ticket.Priority == TicketPriority.High ? "bg-warning" : "bg-secondary-subtle")">
        @ticket.Priority
    </span>
    ```
  - Priority mapping verification:
    - `TicketPriority.Critical` -> `bg-danger`
    - `TicketPriority.High` -> `bg-warning`
    - `TicketPriority.Medium` -> `bg-secondary-subtle`
    - `TicketPriority.Low` -> `bg-secondary-subtle`
  - **Verdict on C4**: Neither `Low` nor `Medium` uses `bg-warning text-dark`. Both map strictly to `bg-secondary-subtle`.

### Challenge 5: Enter key handling & deduplication in `ticket-realtime.js`
- File inspection: `c:\IT_System\src\ItTms.Web\wwwroot\js\ticket-realtime.js`:
  - Enter shortcut (Lines 80-94):
    ```javascript
    commentInput.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
            e.preventDefault();
            const text = commentInput.value.trim();
            if (!text) return;

            if (typeof commentForm.requestSubmit === "function") {
                commentForm.requestSubmit();
            } else {
                commentForm.dispatchEvent(new Event("submit", { cancelable: true }));
            }
        }
    });
    ```
    - `Enter` alone: intercepts default newline (`e.preventDefault()`), validates content, triggers form submit.
    - `Shift + Enter`: condition `!e.shiftKey` is false, event is not intercepted, inserting a newline normally into `<textarea>`.
    - IME composition guard: `!e.isComposing` prevents submitting when confirming Vietnamese Telex/VNI diacritics.
  - Deduplication logic (Lines 160-167 & 192):
    ```javascript
    function appendCommentToView(comment) {
        if (!commentList || !comment) return;

        const commentId = comment.id !== undefined ? comment.id : comment.Id;
        // Chống trùng lặp: nếu đã tồn tại element có data-comment-id này thì bỏ qua
        if (commentId && document.querySelector(`[data-comment-id="${commentId}"]`)) {
            return;
        }
        ...
        const commentHtml = `
            <div class="mb-3 d-flex flex-column ${alignClass}" style="max-width: 80%;" data-comment-id="${commentId || ''}">
    ```
    - Flow A (Ajax optimistic response arrives first): `resData.comment` is appended with `data-comment-id="X"`. When SignalR `ReceiveComment` arrives with ID `X`, `document.querySelector('[data-comment-id="X"]')` evaluates truthy -> returns immediately.
    - Flow B (SignalR event arrives first): `ReceiveComment` appends `data-comment-id="X"`. When Ajax response returns, `document.querySelector` finds existing element -> returns immediately.
    - Result: Exactly 1 bubble rendered in all race conditions.
  - **Verdict on C5**: Fully robust Enter/Shift+Enter handling and deduplication guard.

### Test Suite Execution
- Command: `dotnet test c:\IT_System\ItTms.sln --verbosity minimal`
- Result:
  - `Passed! - Failed: 0, Passed: 342, Skipped: 0, Total: 342, Duration: 32 s - ItTms.Tests.dll (net9.0)`
  - 100% of 342 automated tests pass cleanly with 0 warnings and 0 failures.

---

## 2. Logic Chain

1. From Observation 1, grep searches on all `.js` files in `src/ItTms.Web/wwwroot/js/` confirm 0 instances of `alert(` or `window.alert(`. All user-facing notifications are routed through `showToastCard` in `signalr-notification.js`.
2. From Observation 2, grep searches for `<i class="bi` returned 0 matches in both `signalr-notification.js` and `ticket-realtime.js`. Both scripts utilize SVG vector icons via `<svg class="bi"><use xlink:href="..."/></svg>`, eliminating missing font glyph issues.
3. From Observation 3, grep searches across `src/ItTms.Web/Areas/` found 0 occurrences of `text-primary`. All 20 instances of `text-danger` are legitimate `asp-validation-for` spans. All `#ID` columns use `font-monospace text-muted`. No titles or badges use unauthorized hardcoded colors.
4. From Observation 4, `Staff/Dashboard/Index.cshtml` lines 82-84 and 145-147 conditionally format `Priority == Critical` as `bg-danger`, `Priority == High` as `bg-warning`, and fallback (`Medium` and `Low`) as `bg-secondary-subtle`.
5. From Observation 5, `ticket-realtime.js` explicitly tests `!e.shiftKey` before submitting on `Enter`, allowing multi-line comments via `Shift+Enter`. The `data-comment-id` attribute combined with DOM query deduplication guarantees no duplicate chat bubbles when Ajax and SignalR events interleave.
6. Combined with the 342/342 passing xUnit test suite, the frontend and visual robustness requirements are completely fulfilled.

---

## 3. Caveats

- Browser execution of SignalR WebSockets requires a running server and client browser instance; unit and integration tests simulate the dispatcher and controllers, while the JS logic was audited through source AST/regex review.
- High-latency network simulations (e.g. 5000ms delay on Ajax vs 10ms on SignalR) were analyzed logically; the bidirectional deduplication pattern is symmetric and handles both arrival orders.

---

## 4. Conclusion

All 5 adversarial challenges passed without defect:
1. `alert()` calls: 0 occurrences (PASS)
2. `<i class="bi` font icons in JS: 0 occurrences (PASS)
3. `text-primary` / `text-danger` on titles / `#ID` in `Areas/`: 0 occurrences (PASS)
4. `Staff/Dashboard/Index.cshtml` `Low`/`Medium` priority styling: strictly `bg-secondary-subtle` (PASS)
5. `Enter` / `Shift+Enter` and SignalR/Ajax deduplication logic: verified (PASS)
6. Automated test suite: 342/342 tests PASS (PASS)

**EXPLICIT VERDICT**: `APPROVE`

---

## 5. Verification Method

To independently verify these findings, run the following PowerShell/Bash commands in `c:\IT_System`:

```powershell
# 1. Verify 0 alert() calls in JS
git grep -nE "\balert\s*\(" src/ItTms.Web/wwwroot/js/

# 2. Verify 0 <i class="bi" in JS
git grep -n '<i class="bi' src/ItTms.Web/wwwroot/js/

# 3. Verify no text-primary in Razor Views in Areas
git grep -n 'text-primary' src/ItTms.Web/Areas/

# 4. Verify priority styling in Staff Dashboard
git grep -n 'TicketPriority' src/ItTms.Web/Areas/Staff/Views/Dashboard/Index.cshtml

# 5. Run full test suite
dotnet test c:\IT_System\ItTms.sln
```
Expected output: 0 results for greps 1, 2, and 3; line 82 & 145 for 4; 342 passed for 5.
