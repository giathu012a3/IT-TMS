# Task Assignment: M3 Explorer 1 — 5 Areas MVC & 20 Use Cases Controller Blueprint

## Objective
Design the complete MVC Architecture with 5 Areas covering all 20 Use Cases in `src/ItTms.Web`:
1. **Area `Auth`**:
   - `AuthController`: Login (UC01, checking IsActive and redirecting with error for TC-01), Logout (UC02), ChangePassword (UC03).
2. **Area `User`**:
   - `TicketController`: CreateTicket (UC04, setting priority, SLA DueAt), MyTickets (UC05), ViewTicket (UC06), AddComment (UC07, checking CanComment for TC-08), UploadAttachment (UC08, max 16MB), CloseAndRateTicket (UC09 for TC-09).
3. **Area `Staff`**:
   - `StaffTicketController`: AssignedTickets (UC13, filtering strictly by AssignedToId == currentUser for TC-07), StartProgress (UC10), HoldForInfo (UC11), ResolveTicket (UC12).
4. **Area `Leader`**:
   - `LeaderTicketController`: NewTickets queue, AssignTicket (UC14 for TC-04), RejectTicket (UC15 for TC-06), ReassignTicket (UC16), Dashboard & SLA Reports (UC17).
5. **Area `Admin`**:
   - `UserController`: ListUsers (UC18), CreateUser, UpdateUser, ToggleActive (blocking self-deactivation for TC-10).
   - `RoleController`: ManageRoles (UC19).
   - `AuditController`: SystemLogs (UC20).
6. Routing, dependency injection, and controller base classes.

## References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`

## Output Requirements
Write `mvc_areas_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.


## 2026-10-25T09:43:23Z
You are explorer_m3_1.
Your working directory is: c:\IT_System\.agents\teamwork\explorer_m3_1
Your task assignment is in: c:\IT_System\.agents\teamwork\explorer_m3_1\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, and c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md.
Design the complete MVC Architecture with 5 Areas covering all 20 Use Cases in src/ItTms.Web:
- Area Auth (Login, Logout, ChangePassword)
- Area User (CreateTicket, MyTickets, ViewTicket, AddComment, UploadAttachment, CloseAndRate)
- Area Staff (AssignedTickets, StartProgress, HoldForInfo, Resolve)
- Area Leader (NewTickets, Assign, Reject, Reassign, SLA Dashboard)
- Area Admin (UserManagement, RoleManagement, AuditLogs)
Write mvc_areas_plan.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.
