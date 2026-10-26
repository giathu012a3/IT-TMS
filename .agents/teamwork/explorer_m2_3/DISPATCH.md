# Task Assignment: M2 Explorer 3 — Security Filters, Decorator & State Machine Testing Plan

## Objective
Design the security action filters/decorators and the State Machine testing suite:
1. `AuthorizeRolesAttribute` (Decorator / ActionFilter):
   - Check authentication cookie/claims.
   - Verify user role in allowed roles.
   - Query user active status: if `user.IsActive == false`, reject with 401 Unauthorized or redirect to login with error "Tài khoản đã bị vô hiệu hóa" (Table 7.2 TC-01).
   - If user does not have permission, return 403 Forbidden (Table 7.2 TC-02).
2. Anti-IDOR / BOLA Filter for Ticket Access:
   - Check ticket ownership: Staff cannot open or modify tickets not assigned to them (Table 7.2 TC-07 -> 403 Forbidden).
   - User cannot open tickets created by other users (403 Forbidden).
3. State Machine Unit Testing Matrix (49+ permutations):
   - Design xUnit tests covering all 7 states x 7 actions = 49 combinations.
   - Assert `result.IsSuccess` for valid paths; assert `result.IsFailure` for invalid paths.
   - Test `CanComment` property for each state (false for Resolved, Closed, Rejected).

## References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\explorer_test_survey_3\test_strategy_survey.md`

## Output Requirements
Write your detailed blueprint to `c:\IT_System\.agents\teamwork\explorer_m2_3\security_state_testing_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.


## 2026-10-25T09:04:19Z
[Message] timestamp=2026-10-25T09:04:19Z sender=9e3cad2c-3e7f-49c0-9814-015216856d2e priority=MESSAGE_PRIORITY_HIGH content=You are explorer_m2_3.
Your working directory is: c:\IT_System\.agents\teamwork\explorer_m2_3
Your task assignment is in: c:\IT_System\.agents\teamwork\explorer_m2_3\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, and c:\IT_System\.agents\teamwork\explorer_test_survey_3\test_strategy_survey.md.
Design Security Filters & State Machine Testing Plan:
- AuthorizeRolesAttribute (Decorator / ActionFilter) checking auth, roles, and IsActive status
- Anti-IDOR / BOLA ticket authorization
- State Machine 49-permutation testing matrix in tests/ItTms.Tests
Write security_state_testing_plan.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.
