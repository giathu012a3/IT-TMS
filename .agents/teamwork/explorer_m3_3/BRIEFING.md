# BRIEFING — 2026-10-25T09:52:00Z

## Mission
Thiết kế toàn diện Bootstrap 5 UI/UX & Razor Views Blueprint cho dự án IT-TMS trong src/ItTms.Web (Master Layout, 5 Areas, SLA badges, modals, view models).

## 🔒 My Identity
- Archetype: explorer
- Roles: Read-only investigation, UI/UX architecture, Razor views design, synthesis
- Working directory: c:\IT_System\.agents\teamwork\explorer_m3_3
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: M3

## 🔒 Key Constraints
- Read-only investigation — do NOT implement directly in src/ItTms.Web
- Design Bootstrap 5 UI/UX & Razor Views Blueprint in src/ItTms.Web
- Primary language: Tiếng Việt (technical terms preserved)
- Output files: ui_razor_views_plan.md and handoff.md in working directory
- .agents/teamwork/ holds only metadata (plans, progress, handoffs) — no source/tests/data

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: 2026-10-25T09:43:23Z

## Investigation State
- **Explored paths**:
  - `ORIGINAL_REQUEST.md`, `PROJECT.md`, `spec_inventory.md`, `PROJECT_MEMORY.md`
  - `src/ItTms.Domain` (Entities: Ticket, User, TicketStatus, Comment, Feedback, Attachment, Notification, SystemLog; Enums: TicketPriority, TicketCategory, UserRole, UserStatus)
  - `src/ItTms.Web` (Filters: AuthorizeRolesAttribute, AuthorizeTicketAccessAttribute; Hubs: TicketHub; Layout, wwwroot assets)
  - `tests/ItTms.Tests` (Observed CS0105 duplicate using in SecurityAndRbacAdversarialTests.cs)
- **Key findings**:
  - Bootstrap 5.3 CSS/JS exists locally in `wwwroot/lib/bootstrap/dist`. No external CDN dependency.
  - SignalR Hub exists at `/hubs/ticket`. SignalR client library is needed locally in `wwwroot/lib/microsoft-signalr/`.
  - AuthorizeRolesAttribute redirects unauthorized to `/Auth/Account/Login` or sets `TempData["ErrorMessage"]` for Inactive accounts, and returns `~/Views/Shared/AccessDenied.cshtml` with 403 Forbidden.
  - All 10 verification scenarios from Table 7.2 map directly to specific UI components, state validations, and modals.
- **Unexplored areas**: None. UI/UX Blueprint is fully synthesized.

## Key Decisions Made
- Thiết kế Master `_Layout.cshtml` với Bootstrap 5.3 `data-bs-theme="light/dark"`, LocalStorage persistence, SignalR notification bell, role-based nav.
- Thiết kế chi tiết Razor Views cho cả 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) bao phủ 20 Use Cases.
- Thiết kế hệ thống 6 Modals chuẩn hóa (`AssignModal`, `RejectModal`, `HoldForInfoModal`, `ResolveModal`, `FeedbackModal`, `ReopenModal`).
- Thiết kế Widget Interactive SLA DueAt Preview động theo thời gian thực (Critical: 1h, High: 4h, Medium: 24h, Low: 72h).
- Triển khai thư viện SVG Sprite nội tuyến `_SvgIcons.cshtml` tự hành 100% offline.

## Artifact Index
- c:\IT_System\.agents\teamwork\explorer_m3_3\DISPATCH.md — Task assignment
- c:\IT_System\.agents\teamwork\explorer_m3_3\BRIEFING.md — Persistent memory
- c:\IT_System\.agents\teamwork\explorer_m3_3\progress.md — Heartbeat & progress log
- c:\IT_System\.agents\teamwork\explorer_m3_3\ui_razor_views_plan.md — Comprehensive UI/UX & Razor Views Blueprint
- c:\IT_System\.agents\teamwork\explorer_m3_3\handoff.md — 5-component handoff report
