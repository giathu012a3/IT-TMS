# Task Assignment: M2 Explorer 1 — State Pattern & 7 Concrete States Blueprint

## Objective
Design the complete State Pattern implementation in `src/ItTms.Domain/States/`:
1. `ITicketState` interface and `TicketStateBase` abstract class:
   - Methods: `Assign(User staff)`, `Reassign(User staff)`, `StartProgress()`, `HoldForInfo(string reason)`, `Resolve()`, `Close(Feedback feedback)`, `Reopen(string reason)`, `Reject(string reason)`.
   - Property: `bool CanComment { get; }`.
   - All methods must return Railway-oriented `Result` with explicit `Error` types, NEVER throwing exceptions.
2. 7 Concrete States:
   - `NewState`: can Assign, Reject. CanComment = true.
   - `AssignedState`: can StartProgress, Reassign. CanComment = true.
   - `InProgressState`: can HoldForInfo, Resolve. Chặn Reject (TC-06). CanComment = true.
   - `WaitingState`: can StartProgress. CanComment = true.
   - `ResolvedState`: can Close, Reopen. Chặn Comment: CanComment = false (TC-08). Chặn Close bất hợp pháp (TC-05).
   - `ClosedState`: terminal state. CanComment = false.
   - `RejectedState`: terminal state. CanComment = false.
3. Integration with `Ticket` entity:
   - `Ticket.CurrentState` resolution based on `Ticket.StatusId`.
   - State transition methods on `Ticket` forwarding to `CurrentState` and raising domain events.

## References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`

## Output Requirements
Write your detailed blueprint to `c:\IT_System\.agents\teamwork\explorer_m2_1\state_pattern_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.
