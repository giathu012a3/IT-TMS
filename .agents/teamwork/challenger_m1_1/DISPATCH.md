# Task Assignment: Challenger M1_1 — Concurrency & Conflict Stress Verification

## Objective
Adversarially challenge and verify the concurrency and database invariants in Milestone 1:
1. Write and execute adversarial test cases testing optimistic concurrency conflicts on `Ticket`.
2. Simulate two independent DbContext instances modifying the same ticket concurrently: verify that `DbUpdateConcurrencyException` is reliably thrown on the conflicting update and that the `RowVersion` token changed.
3. Test edge cases in SLA calculations (leap years, DST transitions, zero/negative durations, invalid priorities).
4. Run tests directly and check for race conditions or leaks.
5. Provide an explicit verdict in your handoff report: `APPROVE` or `REQUEST_CHANGES`.

## Mandatory References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m1\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m1\handoff.md`

## Output Requirements
Write your adversarial test report to `c:\IT_System\.agents\teamwork\challenger_m1_1\challenge_report.md` and deliver `handoff.md`.
Explicit verdict MUST be `APPROVE` or `REQUEST_CHANGES`.
Language: Tiếng Việt.

## 2026-10-25T08:30:02Z
You are challenger_m1_1.
Your working directory is: c:\IT_System\.agents\teamwork\challenger_m1_1
Your task assignment is in: c:\IT_System\.agents\teamwork\challenger_m1_1\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, and worker_m1 reports.
Adversarially challenge and stress-test Concurrency and SLA invariants:
- Simulate concurrent updates with 2 DbContext instances to verify DbUpdateConcurrencyException
- Stress-test SLA calculations against edge cases
- Run tests and report empirical results
Provide an explicit verdict in your handoff report: APPROVE or REQUEST_CHANGES.
Write report to c:\IT_System\.agents\teamwork\challenger_m1_1\challenge_report.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.
