# Forensic Integrity Audit Report: Phase 2 Work Product

**Work Product**: Phase 2 Real-Time Comments, Domain Event Dispatch, Floating Card Box Notifications, and UI Neutralization  
**Profile**: General Project / .NET 9 Clean Architecture  
**Integrity Mode**: Development Mode (Ground Truth per `ORIGINAL_REQUEST.md` line 71)  
**Verdict**: **CLEAN**

---

## 1. Observation

### 1.1 Controllers Domain Event Integration (`AddComment`)
- **User TicketController** (`src/ItTms.Web/Areas/User/Controllers/TicketController.cs:286-301`):
  ```csharp
  var comment = commentResult.Value;
  ticket.Comments.Add(comment);
  await _dbContext.Comments.AddAsync(comment);
  await _dbContext.SaveChangesAsync();

  // Gắn Domain Event vào Ticket với đúng CommentId vừa sinh từ CSDL
  ticket.AddDomainEvent(new CommentAddedEvent(
      comment.Id,
      ticket.Id,
      CurrentUserId.Value,
      comment.Content,
      comment.IsInternal,
      comment.CreatedAt));

  await _dbContext.SaveChangesAsync();
  ```
- **Staff TicketController** (`src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs:288-303`):
  ```csharp
  var comment = commentResult.Value;
  ticket.Comments.Add(comment);
  await _dbContext.Comments.AddAsync(comment);
  await _dbContext.SaveChangesAsync();

  // Gắn Domain Event vào Ticket với đúng CommentId vừa sinh từ CSDL
  ticket.AddDomainEvent(new CommentAddedEvent(
      comment.Id,
      ticket.Id,
      CurrentUserId.Value,
      comment.Content,
      comment.IsInternal,
      comment.CreatedAt));

  await _dbContext.SaveChangesAsync();
  ```
- **Leader TicketController** (`src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs:370-385`):
  ```csharp
  var comment = commentResult.Value;
  ticket.Comments.Add(comment);
  await _dbContext.Comments.AddAsync(comment);
  await _dbContext.SaveChangesAsync();

  // Gắn Domain Event vào Ticket với đúng CommentId vừa sinh từ CSDL
  ticket.AddDomainEvent(new CommentAddedEvent(
      comment.Id,
      ticket.Id,
      CurrentUserId!.Value,
      comment.Content,
      comment.IsInternal,
      comment.CreatedAt));

  await _dbContext.SaveChangesAsync();
  ```
- **Two-Phase Dispatcher in DbContext** (`src/ItTms.Infrastructure/Persistence/ItTmsDbContext.cs:31-56`):
  Captures `IHasDomainEvents`, dispatches in-transaction observers (`DispatchInTransactionAsync`) to persist notifications and audit logs, commits to SQLite, then dispatches post-commit handlers (`DispatchPostCommitAsync`) to trigger SignalR real-time broadcast.

### 1.2 Real-time SignalR Dispatcher (`SignalRNotificationDispatcher.cs`)
- **Web Service Registration** (`src/ItTms.Web/Program.cs:23`):
  `builder.Services.AddScoped<INotificationDispatcher, SignalRNotificationDispatcher>();`
  Registers `ItTms.Web.Services.SignalRNotificationDispatcher` which implements authentic broadcasting via `IHubContext<TicketHub>`.
- **Broadcast & Isolation Logic** (`src/ItTms.Web/Services/SignalRNotificationDispatcher.cs:150-183`):
  - Internal notes (`IsInternal == true`): pushed exclusively to `ticket_{id}_internal` and `Ticket_{id}_internal`.
  - Public comments (`IsInternal == false`): pushed to `ticket_{id}` and `Ticket_{id}`.
  - Personal notifications: pushed to `user_{userId}` with counter recalculation `UpdateUnreadNotificationCount`.
  - Non-internal notes notify creator (`ticket.CreatorId`) only if creator is not the commenter.
  - Internal notes strictly exclude creator notification:
    ```csharp
    if (!comment.IsInternal && comment.AuthorUserId != ticket.CreatorId)
    {
        await SendToUserAsync(ticket.CreatorId, ...);
    }
    ```

### 1.3 Client Real-Time Chat & Keyboard Handling (`ticket-realtime.js`)
- **Enter Keydown Handler** (`src/ItTms.Web/wwwroot/js/ticket-realtime.js:80-94`):
  ```javascript
  if (commentInput && commentForm) {
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
  }
  ```
- **Deduplication via `data-comment-id`** (`src/ItTms.Web/wwwroot/js/ticket-realtime.js:163-167, 192`):
  ```javascript
  const commentId = comment.id !== undefined ? comment.id : comment.Id;
  if (commentId && document.querySelector(`[data-comment-id="${commentId}"]`)) {
      return;
  }
  // Rendered container:
  <div ... data-comment-id="${commentId || ''}">
  ```
- **Browser Alert & Font Glitch Scan**:
  - `grep_search` across `wwwroot/js` for `alert(` yielded 0 matches.
  - `grep_search` across `wwwroot/js` for `<i class="bi` yielded 0 matches.
  - All errors and system notifications route through `window.showToastCard(...)` and SVG symbols.

### 1.4 Floating Card Box System (`_ToastAlerts.cshtml` & `site.css`)
- **Razor Template** (`src/ItTms.Web/Views/Shared/_ToastAlerts.cshtml:2-22`):
  Renders fixed top-right container (`#toast-container`) with `.floating-card-box` and semantic types (`floating-card-success`, `floating-card-error`, `floating-card-warning`, `floating-card-info`). Structure includes `.floating-card-icon-box` with SVG symbols, `.floating-card-title`, `.floating-card-message`, `.floating-card-close`, and `.floating-card-progress`.
- **CSS Animation & Hover Pause** (`src/ItTms.Web/wwwroot/css/site.css:1005-1125`):
  - `@keyframes floatingCardSlideIn` (smooth translateX transition).
  - `@keyframes floatingCardProgress` (5000ms linear fill).
  - Hover pause rule:
    ```css
    .floating-card-box:hover .floating-card-progress-bar,
    .floating-card-box.is-paused .floating-card-progress-bar {
      animation-play-state: paused !important;
    }
    ```
  - Shadows use `--shadow-float: 0 12px 32px -4px rgba(17, 24, 39, 0.12), 0 4px 12px -2px rgba(17, 24, 39, 0.06);`.

### 1.5 Unit / Integration Tests (`CommentEventTests.cs`)
- **Inspection of `tests/ItTms.Tests/UnitTests/CommentEventTests.cs`**:
  Contains 6 comprehensive xUnit tests:
  1. `UserAddComment_TriggersDomainEvent_CreatesNotificationAndSystemLog_ReturnsCommentDto`
  2. `StaffAddComment_InternalNote_AddsDomainEvent_ShieldsUserFromNotification_LogsInternalNote`
  3. `LeaderAddComment_Public_TriggersDomainEvent_NotifiesUserAndStaff`
  4. `SignalRNotificationDispatcher_HandleCommentAddedAsync_PublicComment_DispatchesReceiveComment_AndNotifiesCounterparts`
  5. `SignalRNotificationDispatcher_HandleCommentAddedAsync_InternalComment_StrictlyShieldsUser_OnlyDispatchesInternalAndStaffLeader`
  6. `UserAddComment_WhenTicketResolved_IsBlockedByCanComment_DoesNotAddCommentOrEvent`
- **Assertion Quality**: Tests use FluentAssertions to verify concrete database entity properties, DTO JSON fields, mock dispatcher invocations with exact predicates (`e.CommentId == commentInDb.Id && !e.IsInternal`), and verify `Times.Never` for blocked transitions. Zero dummy `Assert.True(true)` assertions found.

### 1.6 Independent Build & Test Execution
- **Command**: `dotnet build c:\IT_System\ItTms.sln`
  - Output:
    ```
    Build succeeded.
        0 Warning(s)
        0 Error(s)
    Time Elapsed 00:00:10.14
    ```
- **Command**: `dotnet test c:\IT_System\ItTms.sln --logger "console;verbosity=normal"`
  - Output:
    ```
    Test Run Successful.
    Total tests: 342
         Passed: 342
     Total time: 33.9518 Seconds
    ```
- **Command**: `dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~Table72"`
  - Output:
    ```
    Test Run Successful.
    Total tests: 20
         Passed: 20
     Total time: 3.8905 Seconds
    ```
- **Command**: `dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~CommentEventTests"`
  - Output:
    ```
    Test Run Successful.
    Total tests: 6
         Passed: 6
     Total time: 3.8820 Seconds
    ```

---

## 2. Logic Chain

1. **Step 1 (Ground Truth Baseline)**: `ORIGINAL_REQUEST.md` (lines 66-123) establishes the acceptance criteria for Phase 2: Enter key shortcut without shift sends comments; backend `AddComment` triggers `CommentAddedEvent` and SignalR broadcast; deduplication via `data-comment-id`; multi-tier floating card notification box; and 100% test pass rate including Table 7.2.
2. **Step 2 (Domain Event Authenticity)**: Observations 1.1 confirm that domain events are not mocked, bypassed, or faked. `Comment.Create` produces a valid entity, which is saved to generate an ID, then `ticket.AddDomainEvent(new CommentAddedEvent(...))` is added to the aggregate root. The DbContext commits data and dispatches to observers and SignalR.
3. **Step 3 (Real-Time Dispatch Authenticity)**: Observation 1.2 confirms that `SignalRNotificationDispatcher` uses real `IHubContext<TicketHub>` methods. Grouping separates public (`ticket_{id}`) from internal (`ticket_{id}_internal`) messages, strictly guarding User visibility.
4. **Step 4 (Frontend Behavior Authenticity)**: Observation 1.3 proves that Enter submits while Shift+Enter newlines, IME is respected (`!e.isComposing`), optimistic display adds `data-comment-id`, and incoming SignalR messages check for this attribute to prevent duplicates.
5. **Step 5 (UI & Notification Fidelity)**: Observation 1.4 confirms the existence of authentic CSS keyframes, hover pause, SVG icon integration, and multi-tier layout without dependency on font icons or `alert()`.
6. **Step 6 (Test Suite Integrity)**: Observation 1.5 confirms that test methods assert real database writes, observer triggers, and SignalR mock invocations rather than self-certifying tautologies.
7. **Step 7 (Empirical Validation)**: Observation 1.6 independently confirms clean build (0 warnings, 0 errors) and 100% test pass rate across 342 tests.

---

## 3. Caveats

- Runtime performance was measured in local Windows developer environment (`net9.0`); real-world WebSocket latency will depend on network infrastructure.
- No other caveats.

---

## 4. Conclusion

The Phase 2 deliverables have been thoroughly inspected and verified from first principles. There are no hardcoded test shortcuts, no facade implementations, no bypassed events, and no fabricated artifacts. The architecture adheres strictly to .NET 9 Clean Architecture, Domain-Driven Design events, and the project requirements.

**EXPLICIT BINARY VERDICT**: **CLEAN**

---

## 5. Verification Method

To independently reproduce this verification:
1. Inspect controller event registration:
   - Check lines 290-300 in `src/ItTms.Web/Areas/User/Controllers/TicketController.cs`
   - Check lines 290-302 in `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs`
   - Check lines 375-385 in `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs`
2. Run clean build:
   ```powershell
   dotnet build c:\IT_System\ItTms.sln
   ```
   (Expect 0 warnings, 0 errors)
3. Execute full test suite:
   ```powershell
   dotnet test c:\IT_System\ItTms.sln
   ```
   (Expect 342 passed)
4. Execute Table 7.2 and CommentEvent tests:
   ```powershell
   dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~Table72"
   dotnet test c:\IT_System\ItTms.sln --filter "FullyQualifiedName~CommentEventTests"
   ```
   (Expect all targeted tests to pass)
