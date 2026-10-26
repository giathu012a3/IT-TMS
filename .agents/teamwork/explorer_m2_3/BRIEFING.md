# BRIEFING — 2026-10-25T09:12:00Z

## Mission
Thiết kế kế hoạch kiểm thử và đặc tả kiến trúc Security Filters (`AuthorizeRolesAttribute`, Anti-IDOR/BOLA) cùng Ma trận 49 hoán vị State Machine cho IT-TMS.

## 🔒 My Identity
- Archetype: explorer
- Roles: Security architecture analysis, State Machine testing strategy design, synthesis
- Working directory: c:\IT_System\.agents\teamwork\explorer_m2_3
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: M2 - Security Filters & State Machine Testing Plan

## 🔒 Key Constraints
- Read-only investigation — do NOT implement production code
- Language Directive: Tiếng Việt as primary communication language, keeping technical terms and code identifiers intact
- Zero Sycophancy & Kolmogorov BLUF-MECE Density (Argon-Omega cognitive kernel)
- Cover all requirements from DISPATCH.md, Table 7.2 (TC-01, TC-02, TC-07), 49 permutations of State Machine, CanComment rules

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: 2026-10-25T09:12:00Z

## Investigation State
- **Explored paths**:
  - `c:\IT_System\src\ItTms.Domain\Entities\Ticket.cs`, `TicketStatus.cs`, `User.cs`
  - `c:\IT_System\src\ItTms.Domain\Enums\` (`RoleNames.cs`, `UserRole.cs`, `UserStatus.cs`)
  - `c:\IT_System\tests\ItTms.Tests` (109 existing tests passed)
  - `c:\IT_System\.agents\teamwork\explorer_test_survey_3\test_strategy_survey.md`
  - `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md` (Table 7.2 Scenarios)
- **Key findings**:
  - `AuthorizeRolesAttribute` must perform real-time DB query for `User.Status == UserStatus.Active` to prevent Stale Session attacks upon account deactivation (Table 7.2 TC-01).
  - `AuthorizeTicketAccessAttribute` prevents OWASP API1:2023 BOLA/IDOR by verifying `AssignedToId` for Staff (TC-07) and `CreatorId` for User, returning 403 Forbidden.
  - State Machine has exactly $7 \times 7 = 49$ permutations: 12 Valid transitions and 37 Forbidden transitions, with `Closed` and `Rejected` being 100% terminal locked states.
  - `CanComment()` invariant strictly allows comments on `New`, `Assigned`, `InProgress`, `Waiting` (4 true) and forbids comments on `Resolved` (TC-08), `Closed`, `Rejected` (3 false).
- **Unexplored areas**: None within the scope of M2 Security & State Testing blueprint.

## Key Decisions Made
- **KDM-001**: Implement `AuthorizeRolesAttribute` as an `IAsyncActionFilter` resolving `IItTmsDbContext` via DI to query `IsActive` in real-time, executing `SignOutAsync()` and redirecting to Login on deactivation.
- **KDM-002**: Implement `AuthorizeTicketAccessAttribute` extracting `ticketId` from RouteData/ActionArguments, enforcing strict ownership boundaries and recording `BOLA_Violation_Attempt` in `SystemLogs`.
- **KDM-003**: Formalize 49-permutation state machine testing via xUnit `[Theory]` and `[MemberData]`, asserting `result.IsSuccess` vs `result.IsFailure` and state preservation.

## Artifact Index
- `c:\IT_System\.agents\teamwork\explorer_m2_3\security_state_testing_plan.md` — Detailed testing & design blueprint
- `c:\IT_System\.agents\teamwork\explorer_m2_3\handoff.md` — 5-component handoff report
- `c:\IT_System\.agents\teamwork\explorer_m2_3\progress.md` — Liveness heartbeat
