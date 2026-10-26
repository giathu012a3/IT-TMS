# BRIEFING — 2026-10-25T10:35:10Z

## Mission
Thẩm định độc lập (Code Review & Adversarial Challenge) tầng ASP.NET Core MVC (5 Areas: Auth, User, Staff, Leader, Admin), 20 Use Cases (UC01-UC20), anti-BOLA/IDOR protection, and security filters trong Milestone 3.

## 🔒 My Identity
- Archetype: reviewer_critic
- Roles: reviewer, critic
- Working directory: c:\IT_System\.agents\teamwork\reviewer_m3_1\
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: Milestone 3
- Instance: 1 of 2

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code
- Check for integrity violations: hardcoded test results, facade implementations, shortcuts, fabricated verification
- Verify 5 Areas: Auth, User, Staff, Leader, Admin
- Verify 20 Use Cases UC01-UC20
- Verify anti-BOLA/IDOR protection and security filters
- Primary language: Tiếng Việt, giữ nguyên technical terms và code identifiers
- Run build and tests independently

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: 2026-10-25T10:35:10Z

## Review Scope
- **Files to review**: `src/ItTms.Web/Areas/`, `src/ItTms.Web/Controllers/`, `src/ItTms.Web/Filters/`, `src/ItTms.Web/Hubs/`, `src/ItTms.Web/Program.cs`, `tests/ItTms.Tests/`
- **Interface contracts**: `ORIGINAL_REQUEST.md`, `PROJECT.md`
- **Review criteria**: correctness, logical completeness, quality, anti-BOLA/IDOR, integrity

## Key Decisions Made
- Confirmed zero integrity violations: real EF Core queries, real SignalR hubs, real BCrypt hashing, real unit/adversarial tests.
- Independently compiled solution: `Build succeeded. 0 Warning(s), 0 Error(s)`.
- Independently ran test suite: 292 passed, 0 failed, 0 skipped in 28s.
- Identified 2 Major adversarial security findings and 2 Minor functional findings.
- Final Verdict: APPROVE.

## Artifact Index
- `handoff.md` — Final 5-part review and verdict report

## Review Checklist
- **Items reviewed**: 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`), 20 Use Cases (UC01-UC20), `AuthorizeRolesAttribute`, `AuthorizeTicketAccessAttribute`, `TicketHub`, `SignalRNotificationDispatcher`, `_Layout.cshtml`, `ticket-realtime.js`.
- **Verdict**: APPROVE
- **Unverified claims**: none; all 292 tests and build independently executed and verified.

## Attack Surface
- **Hypotheses tested**:
  1. Can inactive user authenticate or bypass filter? Tested -> blocked by `AuthorizeRolesAttribute` and `AuthController.Login` (TC-01).
  2. Can user access forbidden area routes? Tested -> HTTP 403 Forbidden returned (TC-02).
  3. Can staff view or join ticket of another staff? Tested -> blocked by `AuthorizeTicketAccessAttribute` and `TicketHub.JoinTicketGroup` (TC-07).
  4. Can staff comment on another staff's ticket via WebSocket `SendComment`? Challenged -> Found boolean tautology flaw in line 159 of `TicketHub.cs`.
  5. Can demoted user retain permissions via stale claims? Challenged -> Found fallback `user.IsInRole` in line 93 of `AuthorizeRolesAttribute.cs`.
- **Vulnerabilities found**:
  1. Flawed condition `if (role == RoleNames.Staff && ticket.AssignedToId != currentUserId && !isStaffOrLeaderOrAdmin)` in `TicketHub.cs:159`.
  2. Stale claims role retention via `|| _allowedRoles.Any(role => user.IsInRole(role))` in `AuthorizeRolesAttribute.cs:93`.
- **Untested angles**: Network-level WebSocket hijacking under high concurrency (deferred to M4 integration/load testing).
