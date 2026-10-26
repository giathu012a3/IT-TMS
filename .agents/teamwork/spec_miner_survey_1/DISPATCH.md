# Task Assignment: Specification & Requirements Mining

## Objective
Extract the complete, authoritative specification inventory for the IT Ticket Management System (.NET 9) project from `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`, `c:\IT_System\PROJECT_MEMORY.md`, and `c:\IT_System\AGENTS.md`.

## Specific Requirements to Extract & Document
1. **20 Use Cases**: Enumerate all 20 Use Cases, mapped to actors and 5 Areas (Auth, User, Staff, Leader, Admin).
2. **8 Core Entities & DB Schema**: Fields, relationships, optimistic concurrency token, SLA tracking timestamps, indexes, seed data requirements.
3. **4 Software Design Patterns**:
   - State Pattern: 7 states, transition rules, Result<T> return types.
   - Observer Pattern: NotificationObserver, SystemLogObserver events and handlers.
   - Decorator / ActionFilter: AuthorizeRolesAttribute implementation rules.
   - Singleton Pattern: configuration / cache / service usage.
4. **Real-time SignalR Hub**: TicketHub methods, events, connection handling for comments & notifications.
5. **Table 7.2 Verification Scenarios**: All 10 scenarios with exact preconditions, actions, and verification criteria.
6. **Documentation Sync**: Requirements for updating `BaoCao_ITTMS_TKPMHDT (4).docx` while preserving 100% of TOC & heading hierarchy.

## Output Requirements
Write your detailed analysis to `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md` and produce a complete `handoff.md` in your working directory.
Language: Tiếng Việt (Vietnamese) as primary communication language, preserving technical terms.


## 2026-10-25T07:55:59Z
Sender: 9e3cad2c-3e7f-49c0-9814-015216856d2e
You are spec_miner_survey_1.
Your working directory is: c:\IT_System\.agents\teamwork\spec_miner_survey_1
Your task assignment is in: c:\IT_System\.agents\teamwork\spec_miner_survey_1\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT_MEMORY.md, and c:\IT_System\AGENTS.md.
Extract and document the comprehensive specification inventory:
1. 20 Use Cases across the 5 Areas (Auth, User, Staff, Leader, Admin) with actor mappings.
2. 8 core entities and EF Core 9 schema requirements, concurrency tokens, SLA tracking timestamps.
3. Software Design Patterns: State Pattern (7 states with Result<T>), Observer Pattern (NotificationObserver, SystemLogObserver), Decorator/ActionFilter (AuthorizeRolesAttribute), Singleton Pattern.
4. SignalR WebSocket Hub (TicketHub) for comments and notifications.
5. All 10 verification scenarios from Table 7.2.
6. Documentation sync requirements for BaoCao_ITTMS_TKPMHDT (4).docx preserving 100% TOC & headings.
Write your findings to c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md and produce handoff.md.
Language: Tiếng Việt. Send message to caller when done.
