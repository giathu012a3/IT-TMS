# Task Assignment: M3 Explorer 2 — SignalR Hub & Real-Time Client Architecture

## Objective
Design the complete SignalR WebSocket integration in `src/ItTms.Web`:
1. `TicketHub` at `/hubs/ticket`:
   - Connection handling: mapping user connection ID to groups `ticket_{id}`, `user_{userId}`, `role_{roleName}`.
   - Client methods: `JoinTicketGroup(int ticketId)`, `LeaveTicketGroup(int ticketId)`.
   - Hub events: `ReceiveComment(CommentDto)`, `ReceiveNotification(NotificationDto)`, `TicketStatusChanged(int ticketId, string status)`.
2. Notification & SignalR Dispatcher:
   - `SignalRNotificationDispatcher` implementing `INotificationDispatcher` to broadcast real-time notifications to connected clients.
3. Client JavaScript:
   - Standalone `signalr.min.js` in `wwwroot/lib/microsoft-signalr/signalr.min.js` (no external CDN dependency).
   - Real-time comment streaming in Ticket detail view (no page refresh, eliminating polling 3s).
   - Real-time notification badge updater in navigation header.

## References
- `c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md`
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md`

## Output Requirements
Write `signalr_realtime_plan.md` and deliver `handoff.md`.
Language: Tiếng Việt.


## 2026-10-25T09:43:23Z
You are explorer_m3_2.
Your working directory is: c:\IT_System\.agents\teamwork\explorer_m3_2
Your task assignment is in: c:\IT_System\.agents\teamwork\explorer_m3_2\DISPATCH.md
Read c:\IT_System\.agents\teamwork\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, and c:\IT_System\.agents\teamwork\spec_miner_survey_1\spec_inventory.md.
Design the SignalR WebSocket Hub & Real-time Client Architecture:
- TicketHub at /hubs/ticket with room grouping (ticket_{id}, user_{userId}, role_{roleName})
- Real-time comment streaming & notification broadcasting
- Local standalone signalr.min.js in wwwroot/lib/microsoft-signalr/
- Elimination of polling 3s anti-pattern
Write signalr_realtime_plan.md and deliver handoff.md.
Language: Tiếng Việt. Send message to caller when done.
