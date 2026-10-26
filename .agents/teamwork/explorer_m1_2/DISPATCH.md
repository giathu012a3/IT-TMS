# Task Assignment: M1 Explorer 2 — Domain Entities & Schema Specifications

## Objective
Design the exact C# definitions for the 8 Core Entities in `src/ItTms.Domain` and their EF Core 9 Fluent API configurations in `src/ItTms.Infrastructure`.

## References
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`
- `c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md`

## Specific Analysis
1. 8 Entities: `User`, `TicketStatus`, `Ticket`, `Comment`, `Feedback`, `Attachment`, `Notification`, `SystemLog`.
2. Property types, foreign keys, navigation properties, nullability.
3. Concurrency token: `Guid ConcurrencyToken` / `byte[] RowVersion` compatible with SQLite.
4. SLA Tracking: `DueAt` (calculated per Priority: Critical 1h, High 4h, Medium 24h, Low 72h), `ResolvedAt`, `ClosedAt`.
5. Covering indexes on frequently queried fields (`CreatedById`, `AssignedToId`, `StatusId`, `CreatedAt`, `DueAt`).

## Output Requirements
Write your detailed recommendations to `c:\IT_System\.agents\teamwork\explorer_m1_2\domain_schema_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.

## 2026-10-25T08:03:55Z
You are explorer_m1_2.
Your working directory is: c:\IT_System\.agents\teamwork\explorer_m1_2
Your task assignment is in: c:\IT_System\.agents\teamwork\explorer_m1_2\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md, and c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md.
Design the exact C# definitions for the 8 Core Entities in src/ItTms.Domain and their EF Core 9 configurations:
- 8 entities (User, TicketStatus, Ticket, Comment, Feedback, Attachment, Notification, SystemLog)
- SQLite ConcurrencyToken handling
- SLA DueAt calculation & timestamps
- Covering indexes
Write your analysis to c:\IT_System\.agents\teamwork\explorer_m1_2\domain_schema_plan.md and produce handoff.md.
Language: Tiếng Việt. Send message to caller when done.
