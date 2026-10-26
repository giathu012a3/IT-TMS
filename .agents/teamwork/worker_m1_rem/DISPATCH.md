# Task Assignment: Worker M1 Remediation — Boundary Fixes & Test Suite Synchronization

## Objective
Implement the 4 remediations and synchronize the test suite according to the plans provided by `explorer_m1_rem_1`, `explorer_m1_rem_2`, and `explorer_m1_rem_3`:
1. `src/ItTms.Infrastructure/Security/PasswordHasherService.cs`:
   - Catch `FormatException` when verifying malformed Base-64 hashes and return `false` (fail-closed).
2. `src/ItTms.Domain/Entities/User.cs`:
   - Validate email syntax via `MailAddress.TryCreate`, rejecting `..` and invalid formats while supporting internal/test domains like `@ittms.local`. Return `USER_EMAIL_INVALID`.
3. `src/ItTms.Domain/Entities/Ticket.cs`:
   - Validate priority using `Enum.IsDefined(typeof(TicketPriority), priority)` in `Ticket.Create`. Return `TICKET_PRIORITY_INVALID`.
4. `src/ItTms.Domain/Entities/Attachment.cs`:
   - Check `string.IsNullOrWhiteSpace(contentType)` returning `ATTACH_CONTENT_TYPE_REQUIRED` and length <= 100 returning `ATTACH_CONTENT_TYPE_TOO_LONG`.
5. `tests/ItTms.Tests/Adversarial/DomainBoundaryAndSecurityAdversarialTests.cs`:
   - Synchronize the 13 test assertions so they assert `result.IsFailure == true` with the expected error codes, and `VerifyPassword` returning `false`.
6. Run `dotnet build c:\IT_System\ItTms.sln` (0 warnings, 0 errors).
7. Run `dotnet test c:\IT_System\ItTms.sln` (109/109 tests PASSED).

## Mandatory References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\explorer_m1_rem_1\remediation_plan.md`
- `c:\IT_System\.agents\teamwork\explorer_m1_rem_2\remediation_plan.md`
- `c:\IT_System\.agents\teamwork\explorer_m1_rem_3\regression_guard_plan.md`

## MANDATORY INTEGRITY WARNING
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

## Output Requirements
Write `c:\IT_System\.agents\teamwork\worker_m1_rem\implementation_report.md` and deliver `handoff.md`.
Language: Tiếng Việt.


## 2026-10-25T08:54:55Z
You are worker_m1_rem.
Your working directory is: c:\IT_System\.agents\teamwork\worker_m1_rem
Your task assignment is in: c:\IT_System\.agents\teamwork\worker_m1_rem\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\explorer_m1_rem_1\remediation_plan.md, c:\IT_System\.agents\teamwork\explorer_m1_rem_2\remediation_plan.md, and c:\IT_System\.agents\teamwork\explorer_m1_rem_3\regression_guard_plan.md.

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Implement the 4 remediations:
1. PasswordHasherService.cs: catch FormatException on verify, return false (fail-closed).
2. User.cs: validate email syntax via MailAddress.TryCreate (reject .. and malformed emails, support local test domains).
3. Ticket.cs: validate priority via Enum.IsDefined(typeof(TicketPriority), priority).
4. Attachment.cs: check string.IsNullOrWhiteSpace(contentType) and length <= 100.
5. Synchronize tests/ItTms.Tests/Adversarial/DomainBoundaryAndSecurityAdversarialTests.cs: update the 13 exposed test assertions to expect failure with standard error codes and VerifyPassword returning false.
6. Verify: Run dotnet build c:\IT_System\ItTms.sln (0 warnings, 0 errors).
7. Run dotnet test c:\IT_System\ItTms.sln (109/109 tests PASSED).
Write c:\IT_System\.agents\teamwork\worker_m1_rem\implementation_report.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.
