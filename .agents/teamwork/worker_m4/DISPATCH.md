## 2026-10-25T13:32:02Z
You are Worker M4 (worker_m4): 22 Views Palette & KPI Neutral Synchronization.
Your working directory is: c:\IT_System\.agents\teamwork\worker_m4
You MUST use send_message to communicate all results, reports, and updates back to your caller (the Project Orchestrator).

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A teamwork_preview_auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

MANDATORY FIRST STEP:
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md under header `## 2026-10-25T13:17:29Z` and c:\IT_System\.agents\teamwork\explorer_survey_2\analysis.md.

YOUR EXCLUSIVE FILE OWNERSHIP:
The 22 Razor Views in `src/ItTms.Web/Areas/` and `Views/Shared`:
- `Areas/Admin/Views/AdminDashboard/Index.cshtml`
- `Areas/Admin/Views/User/Index.cshtml`
- `Areas/Admin/Views/User/Create.cshtml`
- `Areas/Admin/Views/User/Edit.cshtml`
- `Areas/Admin/Views/Role/Index.cshtml`
- `Areas/Admin/Views/Audit/Index.cshtml`
- `Areas/Leader/Views/LeaderDashboard/Index.cshtml`
- `Areas/Leader/Views/LeaderTicket/NewQueue.cshtml`
- `Areas/Leader/Views/LeaderTicket/Index.cshtml`
- `Areas/Leader/Views/LeaderTicket/Details.cshtml`
- `Areas/Staff/Views/Dashboard/Index.cshtml`
- `Areas/Staff/Views/StaffTicket/Assigned.cshtml`
- `Areas/Staff/Views/StaffTicket/Details.cshtml`
- `Areas/Staff/Views/StaffTicket/History.cshtml`
- `Areas/User/Views/Dashboard/Index.cshtml`
- `Areas/User/Views/Ticket/Index.cshtml`
- `Areas/User/Views/Ticket/Create.cshtml`
- `Areas/User/Views/Ticket/Details.cshtml`
- `Areas/Auth/Views/Auth/ChangePassword.cshtml`
- `Views/Shared/AccessDenied.cshtml`
- `Views/Shared/Error.cshtml`
- `Views/Shared/_StatusBadge.cshtml`
DO NOT edit `site.css`, `_ToastAlerts.cshtml`, `_Layout.cshtml`, `Login.cshtml`, or backend controllers.

YOUR TASK SPECIFICATION:
Apply the exact replacements cataloged in `explorer_survey_2/analysis.md`:
1. Remove all hardcoded `text-primary`, `text-danger`, `text-warning`, `text-success` from page titles (`h1`–`h6`) and modal headers (`modal-title`).
2. Standardize ticket `#ID` code formatting across all tables: replace `text-primary` or `text-danger` with neutral monospace `font-monospace text-muted`.
3. Fix priority badge logic in `Staff/Dashboard/Index.cshtml:82`: ensure `Low` and `Medium` use `bg-secondary-subtle`, while only `High`/`Critical` use warning colors.
4. Standardize KPI cards across `AdminDashboard`, `Staff/Dashboard`, and `User/Dashboard`: remove rainbow inline icon classes (`text-primary`, `text-success`, `text-warning`) to match the neutral style of `LeaderDashboard`.
5. Run `dotnet build c:\IT_System\ItTms.sln` to ensure 0 build errors.

DELIVERABLE:
Write a comprehensive report to `c:\IT_System\.agents\teamwork\worker_m4\handoff.md` with:
- Files modified
- Exact build command and execution output
- Verification evidence that git grep finds 0 unwanted text-primary/text-danger on headers or #ID columns
Then send a completion message to caller.
