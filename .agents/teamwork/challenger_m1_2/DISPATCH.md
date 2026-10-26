# Task Assignment: Challenger M1_2 — Domain Invariants & Security Adversarial Verification

## Objective
Adversarially challenge and verify domain entity rules and security mechanisms in Milestone 1:
1. Test domain boundary limits: negative priorities, null/empty titles, oversized attachment sizes (>16MB), invalid email formats.
2. Test password hasher against timing attacks, empty passwords, unicode characters, and verify salt uniqueness.
3. Test DataSeeder idempotency: call `SeedAsync()` multiple times and verify that duplicate records are NOT created and data is preserved.
4. Run tests directly and report empirical findings.
5. Provide an explicit verdict in your handoff report: `APPROVE` or `REQUEST_CHANGES`.

## Mandatory References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m1\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m1\handoff.md`

## Output Requirements
Write your adversarial test report to `c:\IT_System\.agents\teamwork\challenger_m1_2\challenge_report.md` and deliver `handoff.md`.
Explicit verdict MUST be `APPROVE` or `REQUEST_CHANGES`.
Language: Tiếng Việt.

## 2026-10-25T08:30:02Z
From: 9e3cad2c-3e7f-49c0-9814-015216856d2e
Content:
You are challenger_m1_2.
Your working directory is: c:\IT_System\.agents\teamwork\challenger_m1_2
Your task assignment is in: c:\IT_System\.agents\teamwork\challenger_m1_2\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, and worker_m1 reports.
Adversarially challenge and test Domain Invariants and Security:
- Test boundary limits (negative priority, oversized attachments >16MB, invalid email)
- Test PasswordHasher (salt uniqueness, timing resistance)
- Test DataSeeder idempotency (calling SeedAsync repeatedly must not duplicate records)
- Run tests and report empirical results
Provide an explicit verdict in your handoff report: APPROVE or REQUEST_CHANGES.
Write report to c:\IT_System\.agents\teamwork\challenger_m1_2\challenge_report.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.
