# BRIEFING — 2026-10-25T10:32:00Z

## Mission
Conduct empirical adversarial verification and stress testing on SignalR WebSocket Hub (`TicketHub`) and `SignalRNotificationDispatcher` in IT-TMS (.NET 9), testing BOLA/IDOR, connection lifecycle, and concurrency.

## 🔒 My Identity
- Archetype: challenger
- Roles: critic, specialist
- Working directory: c:\IT_System\.agents\teamwork\challenger_m3_1\
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: M3
- Instance: 1 of 2

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code directly; write adversarial test suite in `tests/ItTms.Tests/SignalR/`.
- Must empirically run and verify all tests via `dotnet test c:\IT_System\ItTms.sln`.
- Report findings with clear evidence chain and provide explicit verdict (APPROVE or REQUEST_CHANGES).

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: 2026-10-25T10:29:10Z

## Review Scope
- **Files to review**:
  - `src/ItTms.Web/Hubs/TicketHub.cs`
  - `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs`
  - `src/ItTms.Infrastructure/Realtime/SignalRNotificationDispatcher.cs`
  - `tests/ItTms.Tests/UnitTests/Web/TicketHubTests.cs`
- **Interface contracts**: `PROJECT.md` Section "ItTms.Web ↔ SignalR Hub"
- **Review criteria**: Correctness, BOLA/IDOR resilience, Concurrency safety, State-constraint enforcement (TC-08).

## Key Decisions Made
- Discovered critical BOLA flaw in `TicketHub.SendComment`: `if (role == RoleNames.Staff && ticket.AssignedToId != currentUserId && !isStaffOrLeaderOrAdmin)` where `!isStaffOrLeaderOrAdmin` is tautologically false for Staff, allowing any unassigned staff to comment on any ticket.
- Designing targeted empirical tests in `tests/ItTms.Tests/SignalR/` to reproduce this and verify lifecycle, BOLA, concurrency, and dispatcher behavior.

## Artifact Index
- `c:\IT_System\.agents\teamwork\challenger_m3_1\DISPATCH.md` — Task requirements & input files
- `c:\IT_System\.agents\teamwork\challenger_m3_1\progress.md` — Liveness & status tracking
- `c:\IT_System\.agents\teamwork\challenger_m3_1\handoff.md` — Final handoff report

## Attack Surface
- **Hypotheses tested**:
  - H1: Unassigned Staff can call `SendComment` on a ticket they are not assigned to without being blocked (BOLA violation).
  - H2: Non-creator User cannot call `SendComment` or `JoinTicketGroup`.
  - H3: Comments cannot be added to Closed, Resolved, or Rejected tickets (Table 7.2 TC-08).
  - H4: SignalR connection lifecycle properly handles Join/Leave for internal and public groups.
  - H5: High concurrency connections and comments do not crash the Hub or cause database deadlocks/race conditions.
- **Vulnerabilities found**: TBD via empirical execution
- **Untested angles**: Concurrency under load, WebSocket disconnect cleanup, large payloads.

## Loaded Skills
- **Source**: `c:\IT_System\.agents\skills\testing-qa\unit-testing-tdd\SKILL.md`
  - **Local copy**: `c:\IT_System\.agents\skills\testing-qa\unit-testing-tdd\SKILL.md`
  - **Core methodology**: AAA pattern, isolated unit/integration testing, mocking SignalR hub contexts.
- **Source**: `c:\IT_System\.agents\skills\backend\backend-security-owasp\SKILL.md`
  - **Local copy**: `c:\IT_System\.agents\skills\backend\backend-security-owasp\SKILL.md`
  - **Core methodology**: BOLA/IDOR prevention, resource-based authorization, least privilege enforcement.
