# Task Assignment: Challenger M2_2 — Security Filters, RBAC & IDOR Adversarial Challenger

## 2026-10-25T09:31:47Z
You are challenger_m2_2.
Your working directory is: c:\IT_System\.agents\teamwork\challenger_m2_2
Your task assignment is in: c:\IT_System\.agents\teamwork\challenger_m2_2\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, and worker_m2 reports.
Adversarially challenge and test Security Filters and RBAC:
- Deactivated user session rejection (TC-01)
- Unauthorized role route access 403 (TC-02)
- Anti-BOLA/IDOR protection (TC-07)
- Admin self-deactivation block (TC-10)
- Run tests and report empirical results
Provide an explicit verdict in your handoff report: APPROVE or REQUEST_CHANGES.
Write challenge_report.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.

## Objective
Adversarially challenge and stress-test the security action filters:
1. Simulate deactivated user session: user is active at login, then deactivated in DB -> verify next request is immediately blocked (Table 7.2 TC-01).
2. Simulate role elevation attacks / unauthorized route access (User accessing Leader route -> 403 Forbidden, Table 7.2 TC-02).
3. Simulate BOLA/IDOR attacks: Staff 1 attempting to view/modify Ticket assigned to Staff 2 -> 403 Forbidden (Table 7.2 TC-07).
4. Simulate Admin self-deactivation attempt -> verify blocked (Table 7.2 TC-10).
5. Run tests directly and report empirical findings.
6. Provide an explicit verdict in your handoff report: `APPROVE` or `REQUEST_CHANGES`.

## Mandatory References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m2\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m2\handoff.md`

## Output Requirements
Write `challenge_report.md` and deliver `handoff.md`.
Explicit verdict MUST be `APPROVE` or `REQUEST_CHANGES`.
Language: Tiếng Việt.
