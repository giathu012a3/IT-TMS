# BRIEFING — 2026-10-25T10:30:00Z

## Mission
Thực hiện kiểm thử thực nghiệm đối kháng (Empirical Adversarial Verification) trên tầng MVC Controllers và Security Filters cho 10 kịch bản kiểm nghiệm Bảng 7.2 (TC-01 đến TC-10), viết và thực thi integration tests trong `tests/ItTms.Tests/Controllers/`, bảo đảm giải pháp đạt 0 warnings, 0 errors, 100% tests pass và đưa ra phán quyết nghiệm thu.

## 🔒 My Identity
- Archetype: Empirical Challenger
- Roles: critic, specialist
- Working directory: c:\IT_System\.agents\teamwork\challenger_m3_2\
- Original parent: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Milestone: Milestone 3 (MVC Controllers, Security Filters, Integration Verification)
- Instance: 1 of 1

## 🔒 Key Constraints
- Review-only — do NOT modify implementation code. Any failure found must be reported in handoff.
- Viết test đối kháng kiểm chứng thực tế trong `tests/ItTms.Tests/Controllers/`.
- Tuân thủ hạt nhân nhận thức `#Argon-Omega`: Mật độ cao, zero-fluff, kiểm chứng thực nghiệm thay vì chỉ đọc code.
- Ngôn ngữ giao tiếp: Tiếng Việt, giữ nguyên technical terms và code identifiers.
- Đảm bảo `dotnet build c:\IT_System\ItTms.sln` đạt 0 warnings, 0 errors.
- Đảm bảo `dotnet test c:\IT_System\ItTms.sln` đạt 100% pass (bao gồm toàn bộ tests cũ và mới).
- Không được ghi code/tests vào `.agents/teamwork/`.

## Current Parent
- Conversation ID: 9e3cad2c-3e7f-49c0-9814-015216856d2e
- Updated: 2026-10-25T10:30:00Z

## Review Scope
- **Files to review**:
  - `src/ItTms.Web/Areas/Auth/Controllers/AuthController.cs`
  - `src/ItTms.Web/Areas/User/Controllers/TicketController.cs`
  - `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs`
  - `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs`
  - `src/ItTms.Web/Areas/Admin/Controllers/UserController.cs`
  - `src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs`
  - `src/ItTms.Web/Filters/AuthorizeTicketAccessAttribute.cs`
  - `src/ItTms.Web/Hubs/TicketHub.cs`
- **Interface contracts**: `PROJECT.md`, `PROJECT_MEMORY.md`
- **Review criteria**:
  - TC-01: Deactivated user login blocked.
  - TC-02: User access Leader route blocked (403).
  - TC-03: User creates Ticket -> New state, SLA DueAt calculated, notification to Leader.
  - TC-04: Leader assigns Ticket -> Assigned state, notifications to Staff and User.
  - TC-05: Staff closes New Ticket -> Rejected by State Pattern.
  - TC-06: Leader rejects InProgress Ticket -> Rejected by State Pattern.
  - TC-07: Staff opens other's ticket -> Rejected by AuthorizeTicketAccessAttribute (403).
  - TC-08: Comment on Resolved ticket -> Rejected by CanComment == false.
  - TC-09: Close Resolved ticket with feedback -> Closed state, rating saved, audit logged.
  - TC-10: Admin self-deactivation -> Blocked by domain logic.

## Key Decisions Made
- Xây dựng bộ Integration Tests trực tiếp gọi các MVC Controller Actions với `ActionExecutingContext` / HttpContext được cấu hình đầy đủ Claims, Services, TempData, ModelBinding để giả lập chính xác request pipeline ASP.NET Core MVC mà không phụ thuộc HTTP server ngoài.
- Dùng SQLite InMemory connection mở liên tục để kiểm chứng thực thi EF Core 9 thực sự (Foreign Keys, Constraints, ConcurrencyToken, Events, Database Persistence).

## Artifact Index
- `tests/ItTms.Tests/Controllers/Table72MvcControllerIntegrationTests.cs` — Bộ test tích hợp kiểm chứng 10 kịch bản Bảng 7.2.
- `c:\IT_System\.agents\teamwork\challenger_m3_2\progress.md` — Liveness heartbeat.
- `c:\IT_System\.agents\teamwork\challenger_m3_2\handoff.md` — Handoff report với phán quyết cuối cùng.

## Attack Surface
- **Hypotheses tested**: 10 kịch bản Bảng 7.2 được thực thi qua Controller Actions xem có bị bypass security filter, state machine, hay logic kiểm soát không.
- **Vulnerabilities found**: Đang kiểm tra.
- **Untested angles**: Đang bắt đầu.

## Loaded Skills
- **Source**: `c:\IT_System\.agents\skills\testing-qa\unit-testing-tdd\SKILL.md`
- **Local copy**: `c:\IT_System\.agents\teamwork\challenger_m3_2\unit-testing-tdd-skill.md`
- **Core methodology**: Classical Detroit state verification over fragile over-mocking, Real DB/InMemory SQLite, boundary & invariant falsification.
