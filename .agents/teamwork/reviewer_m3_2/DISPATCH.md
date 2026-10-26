# Dispatch Instructions: reviewer_m3_2

## Objective
Thẩm định độc lập (Code Review & Verification) cho hạ tầng SignalR WebSocket Hub (`TicketHub.cs` tại `/hubs/ticket`), các client scripts cục bộ (`signalr.min.js`, `signalr.js`, `ticket-realtime.js`, `signalr-notification.js`), hệ thống giao diện Bootstrap 5 (Dark/Light mode, SVG icons, SLA badge, Toast alerts).

## Input Files
- `c:\IT_System\ORIGINAL_REQUEST.md` (BẮT BUỘC ĐỌC)
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m3\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m3\handoff.md`
- Source files: `src/ItTms.Web/Hubs/`, `src/ItTms.Web/wwwroot/`, `src/ItTms.Web/Views/Shared/`, `src/ItTms.Web/Program.cs`

## Working Directory
`c:\IT_System\.agents\teamwork\reviewer_m3_2\`

## Requirements & Scope
1. Kiểm tra kiến trúc SignalR WebSocket Hub, phương thức join group, gửi broadcast `ReceiveComment`, `ReceiveTicketStatusChanged`, `ReceiveNotification`.
2. Kiểm tra việc xóa bỏ 100% polling 3s của hệ thống cũ. Đảm bảo client script SignalR được lưu trữ cục bộ tại `wwwroot/lib/microsoft-signalr/`, không dùng CDN.
3. Kiểm tra các Views Razor và Shared Layout, dark/light theme toggle, SLA badge hiển thị thời hạn.
4. Chạy lệnh kiểm tra độc lập:
   - `dotnet build c:\IT_System\ItTms.sln`
   - `dotnet test c:\IT_System\ItTms.sln`
5. Ghi nhận `handoff.md` theo chuẩn 5 phần kèm phán quyết rõ ràng: `APPROVE` hoặc `REQUEST_CHANGES`.
6. Gửi thông điệp kết quả về cho orchestrator.

## 2026-10-25T10:29:10Z
You are reviewer_m3_2. Your working directory is c:\IT_System\.agents\teamwork\reviewer_m3_2\.
FIRST: Read c:\IT_System\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\reviewer_m3_2\DISPATCH.md, and c:\IT_System\.agents\teamwork\worker_m3\handoff.md.
Review the SignalR WebSocket Hub at /hubs/ticket, local client scripts in wwwroot/lib/microsoft-signalr/ (0 CDN dependencies), removal of legacy 3s polling, and Bootstrap 5 UI/UX (dark/light theme, SLA badge, SVG icons, toast alerts).
Run `dotnet build c:\IT_System\ItTms.sln` and `dotnet test c:\IT_System\ItTms.sln` independently.
Write handoff.md in your working directory with your 5-part report and explicit verdict (APPROVE or REQUEST_CHANGES). Send your completion message back to orchestrator.
