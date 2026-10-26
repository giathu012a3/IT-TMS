# Dispatch Instructions: challenger_m3_1

## Objective
Kiểm thử đối kháng và kiểm chứng thực nghiệm (Adversarial Testing & Stress Testing) đối với SignalR WebSocket Hub (`TicketHub`) và Real-time Notification/Comment Dispatching trong ASP.NET Core .NET 9.

## Input Files
- `c:\IT_System\ORIGINAL_REQUEST.md` (BẮT BUỘC ĐỌC)
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m3\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m3\handoff.md`

## Working Directory
`c:\IT_System\.agents\teamwork\challenger_m3_1\`

## Requirements & Scope
1. Viết các bài kiểm thử thực nghiệm (thêm vào `tests/ItTms.Tests/SignalR/` hoặc bộ kiểm thử tích hợp) để kiểm tra:
   - Client kết nối và JoinTicketGroup, LeaveTicketGroup.
   - Thử nghiệm BOLA/IDOR trên SignalR: Client không có quyền truy cập ticket cố gắng join group hoặc gửi comment -> Hub phải từ chối/báo lỗi.
   - Dispatch thông báo qua `SignalRNotificationDispatcher` tới đúng User và Role group.
   - Concurrency stress test: Nhiều client kết nối và gửi comment đồng thời vào cùng một TicketHub.
2. Chạy `dotnet test c:\IT_System\ItTms.sln` và đảm bảo toàn bộ tests đều pass, build 0 warnings/0 errors.
3. Ghi nhận `handoff.md` kèm phán quyết rõ ràng: `APPROVE` hoặc `REQUEST_CHANGES`.
4. Gửi thông điệp kết quả về cho orchestrator.

## 2026-10-25T10:29:10Z
[Message] sender=9e3cad2c-3e7f-49c0-9814-015216856d2e priority=MESSAGE_PRIORITY_HIGH
You are challenger_m3_1. Your working directory is c:\IT_System\.agents\teamwork\challenger_m3_1\.
FIRST: Read c:\IT_System\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\challenger_m3_1\DISPATCH.md, and c:\IT_System\.agents\teamwork\worker_m3\handoff.md.
Conduct empirical adversarial verification on SignalR WebSocket Hub (`TicketHub`) and `SignalRNotificationDispatcher`:
- Verify connection lifecycle (JoinTicketGroup, LeaveTicketGroup).
- Test BOLA/IDOR on SignalR Hub: unauthorized users attempting to join groups or send comments.
- Test concurrent client connections and streaming under stress.
Write and execute test cases in `tests/ItTms.Tests/SignalR/`.
Ensure `dotnet build c:\IT_System\ItTms.sln` (0 warnings, 0 errors) and `dotnet test c:\IT_System\ItTms.sln` pass.
Write handoff.md with explicit verdict (APPROVE or REQUEST_CHANGES). Send completion message back to orchestrator.
