# Task Assignment: M2 Explorer 2 — Observer Pattern & Domain Events Blueprint

## Objective
Design the complete Observer Pattern and Domain Event pipeline:
1. Domain Events in `src/ItTms.Domain/Events/`:
   - `TicketCreatedEvent(int ticketId, int creatorId, TicketPriority priority)`
   - `TicketAssignedEvent(int ticketId, int staffId, int assignedById)`
   - `TicketStatusChangedEvent(int ticketId, int oldStatusId, int newStatusId, int changedById, string? reason)`
   - `TicketCommentAddedEvent(int ticketId, int commentId, int authorId)`
   - `TicketClosedEvent(int ticketId, int closedById, int rating)`
2. Observers in `src/ItTms.Application/Observers/`:
   - `NotificationObserver`:
     - On TicketCreated: dispatch notification to Leaders.
     - On TicketAssigned: dispatch notification to Staff and User (TC-04).
     - On TicketClosed: dispatch notification to Staff.
     - On TicketCommentAdded: notify ticket participant.
   - `SystemLogObserver`:
     - Record immutable `SystemLog` entry for every state change, assignment, user deactivation, and close event.
3. Event Dispatcher & EntityBase DomainEvents collection.

## References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`

## Output Requirements
Write your detailed blueprint to `c:\IT_System\.agents\teamwork\explorer_m2_2\observer_pattern_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.

## 2026-10-25T09:04:19Z
You are explorer_m2_2.
Your working directory is: c:\IT_System\.agents\teamwork\explorer_m2_2
Your task assignment is in: c:\IT_System\.agents\teamwork\explorer_m2_2\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, and c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md.
Design the Observer Pattern & Domain Events pipeline:
- Domain Events in src/ItTms.Domain/Events/
- NotificationObserver & SystemLogObserver in src/ItTms.Application/Observers/
- Domain event dispatching via MediatR / In-Memory Event Dispatcher in Application/Infrastructure
Write observer_pattern_plan.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.
