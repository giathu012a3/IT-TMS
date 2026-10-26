# Worker M1 Dispatch Log

## 2026-10-25T13:32:02Z
- Task: Backend Realtime Engine & Events
- Exclusive file ownership:
  - `src/ItTms.Web/Areas/User/Controllers/TicketController.cs`
  - `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs`
  - `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs`
  - `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs` (note: let's verify exact file location: `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs` or `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs` - we will search for where it is located)
  - `tests/ItTms.Tests/UnitTests/CommentEventTests.cs` (or integration test files for comments)
- Requirements:
  1. Update `AddComment` in `User/TicketController.cs`, `StaffTicketController.cs`, and `LeaderTicketController.cs`:
     - Enforce domain event: when comment is created via `Comment.Create(...)` and added to `ticket.Comments`, invoke `ticket.AddDomainEvent(new CommentAddedEvent(comment.Id, ticket.Id, authorUserId, content, isInternal, createdAt))` (check exact constructor).
     - Ensure `IsInternal` is strictly false for User, but allowed for Staff and Leader.
     - Save changes via `_context.SaveChangesAsync()`.
     - In Ajax responses (`IsAjaxRequest`), return JSON containing `success = true, message = "...", comment = commentDto` where `CommentDto` has `id, ticketId, authorId, authorName, authorRole, content, createdAt, isInternal`.
  2. Update `SignalRNotificationDispatcher.cs`:
     - In `HandleCommentAddedAsync(CommentAddedEvent notification)`:
       - Push `ReceiveComment` to group `$"ticket_{ticket.Id}"` (or `$"ticket_{ticket.Id}_internal"` if `notification.Comment.IsInternal`).
       - If `notification.Comment.IsInternal == false`, dispatch `ReceiveNotification` (bell notification) to relevant counterpart users (e.g., if User comments, notify assigned Staff and Leader; if Staff comments, notify User and Leader; if Leader comments, notify User and assigned Staff).
       - If `notification.Comment.IsInternal == true`, NEVER notify User; only notify Staff and Leader.
  3. Add unit/integration tests in `tests/ItTms.Tests/`:
     - Test that `AddComment` adds `CommentAddedEvent`.
     - Test that `SignalRNotificationDispatcher.HandleCommentAddedAsync` routes to correct groups and shields User from internal comments.
  4. Run `dotnet build c:\IT_System\ItTms.sln` and `dotnet test c:\IT_System\ItTms.sln`. Ensure 0 errors and all tests PASS.
