# Task Assignment: M1 Explorer 1 — Solution Scaffolding & Project Dependencies

## Objective
Design the exact scaffolding plan for the .NET 9 Solution (`ItTms.sln`) with 5 projects according to Clean Architecture:
- `src/ItTms.Domain` (classlib, net9.0)
- `src/ItTms.Application` (classlib, net9.0, refs Domain)
- `src/ItTms.Infrastructure` (classlib, net9.0, refs Application, Domain)
- `src/ItTms.Web` (web / mvc, net9.0, refs Application, Infrastructure, Domain)
- `tests/ItTms.Tests` (xunit, net9.0, refs Web, Infrastructure, Application, Domain)

## References
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md`
- `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`

## Specific Analysis
1. Exact CLI commands (`dotnet new sln`, `dotnet new classlib`, `dotnet new mvc`, `dotnet new xunit`, `dotnet sln add`).
2. Exact NuGet package list and versions compatible with .NET 9 (`Microsoft.EntityFrameworkCore.Sqlite`, `Microsoft.EntityFrameworkCore.Design`, `Microsoft.EntityFrameworkCore.Tools`, `Microsoft.AspNetCore.SignalR.Client`, etc.).
3. Project references and directory structure.
4. Compiler settings: `<Nullable>enable</Nullable>`, `<TreatWarningsAsErrors>false</TreatWarningsAsErrors>` or clean warning-free build.

## Output Requirements
Write your detailed recommendations to `c:\IT_System\.agents\teamwork\explorer_m1_1\scaffolding_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.


## 2026-10-25T08:03:55Z
You are explorer_m1_1.
Your working directory is: c:\IT_System\.agents\teamwork\explorer_m1_1
Your task assignment is in: c:\IT_System\.agents\teamwork\explorer_m1_1\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\explorer_codebase_survey_2\codebase_assessment.md, and c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md.
Design the exact scaffolding plan for the .NET 9 Solution (ItTms.sln) with 5 projects:
- CLI commands
- NuGet packages and versions for net9.0
- Project references and directory layout
- Warning-free build configuration
Write your analysis to c:\IT_System\.agents\teamwork\explorer_m1_1\scaffolding_plan.md and produce handoff.md.
Language: Tiếng Việt. Send message to caller when done.
