# Progress Log - Worker M1 (Backend Realtime Engine & Events)

Last visited: 2026-10-25T14:07:30Z

## Status: COMPLETED

### Completed Steps:
- [x] Received dispatch instructions and constraints.
- [x] Initialized DISPATCH.md and BRIEFING.md.
- [x] Read ORIGINAL_REQUEST.md (2026-10-25T13:17:29Z) and explorer_survey_1 analysis.md.
- [x] Inspected source files under ownership: controllers, SignalR dispatcher, event models, existing tests.
- [x] Updated `AddComment` in `User/TicketController.cs`, `StaffTicketController.cs`, and `LeaderTicketController.cs`:
  - Added Domain Event `ticket.AddDomainEvent(new CommentAddedEvent(...))` after comment creation with actual generated identity ID.
  - Enforced `isInternal: false` strictly for User, permitted for Staff and Leader.
  - Implemented 2-phase commit to ensure transactional persistence of Notification and SystemLog before SignalR push.
  - Returned rich Ajax JSON response with `CommentDto` containing `id, ticketId, authorId, authorUserId, authorName, authorRole, content, createdAt, isInternal, formattedTime`.
- [x] Updated `SignalRNotificationDispatcher.HandleCommentAddedAsync`:
  - Dispatched `ReceiveComment` to `ticket_{id}` (or `ticket_{id}_internal` for internal notes).
  - Implemented counterpart notification routing via `SendToUserAsync`:
    - Public comments notify assigned Staff and active Leaders (when User comments) or User and Leaders (when Staff comments) or User and Staff (when Leader comments).
    - Internal comments strictly shield User (never notify User); notify assigned Staff and active Leaders.
  - Fixed EF Core query translation by replacing unmapped `u.IsActive` with `u.Status == UserStatus.Active`.
- [x] Created `tests/ItTms.Tests/UnitTests/CommentEventTests.cs` (6 test cases covering Domain Events, In-Transaction Observers, Ajax DTO response, SignalR room and user routing, internal comment shielding, and CanComment guard).
- [x] Ran `dotnet build c:\IT_System\ItTms.sln` -> 0 warnings, 0 errors.
- [x] Ran `dotnet test c:\IT_System\ItTms.sln` -> 342/342 tests PASSED (100%).
- [x] Prepared `handoff.md` for independent audit.
