# Task Assignment: M1 Explorer 3 — DbContext, Migrations & Data Seeder Design

## Objective
Design the `ItTmsDbContext`, EF Core 9 Migrations approach, SQLite database configuration, and the DataSeeder service.

## References
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`
- `c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md`

## Specific Analysis
1. `ItTmsDbContext` implementation with `DbSet` for all 8 entities and `IItTmsDbContext` abstraction in Application layer.
2. Connection string configuration for SQLite (`Data Source=ittms.db`).
3. Automated database creation/migration on startup (`db.Database.MigrateAsync()` / `EnsureCreatedAsync()`).
4. DataSeeder:
   - 7 Ticket Statuses: `New`, `Assigned`, `InProgress`, `Waiting`, `Resolved`, `Closed`, `Rejected`.
   - Seed test accounts with PBKDF2/BCrypt hashed passwords for 4 roles:
     - Admin: `admin@ittms.local` / `Admin@123`
     - Leader: `leader@ittms.local` / `Leader@123`
     - Staff: `staff1@ittms.local`, `staff2@ittms.local` / `Staff@123`
     - User: `user1@ittms.local`, `user2@ittms.local` / `User@123`
   - Active status: test accounts must be active (`IsActive = true`), plus 1 deactivated account for Table 7.2 TC-01 (`deactivated@ittms.local`).

## Output Requirements
Write your detailed recommendations to `c:\IT_System\.agents\teamwork\explorer_m1_3\seeder_dbcontext_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.

## 2026-10-25T08:03:56Z
You are explorer_m1_3.
Your working directory is: c:\IT_System\.agents\teamwork\explorer_m1_3
Your task assignment is in: c:\IT_System\.agents\teamwork\explorer_m1_3\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md, and c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md.
Design the ItTmsDbContext, EF Core 9 Migrations approach, SQLite database setup, and the DataSeeder service:
- DbContext & IItTmsDbContext abstraction
- SQLite connection string & automated migration on startup
- DataSeeder with 7 statuses, 4 roles, test accounts (including deactivated account for Table 7.2 TC-01)
Write your analysis to c:\IT_System\.agents\teamwork\explorer_m1_3\seeder_dbcontext_plan.md and produce handoff.md.
Language: Tiếng Việt. Send message to caller when done.
