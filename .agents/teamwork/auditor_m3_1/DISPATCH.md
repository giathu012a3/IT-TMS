# Dispatch Instructions: auditor_m3_1 (Forensic Auditor)

## Objective
Kiểm toán độc lập toàn diện về tính trung thực và toàn vẹn mã nguồn (Forensic Integrity Audit) cho toàn bộ thành quả Milestone 3 (MVC 5 Areas, SignalR Hub, Views, Controllers, Test suites).

## Input Files
- `c:\IT_System\ORIGINAL_REQUEST.md` (BẮT BUỘC ĐỌC)
- `c:\IT_System\PROJECT.md`
- `c:\IT_System\.agents\teamwork\worker_m3\implementation_report.md`
- `c:\IT_System\.agents\teamwork\worker_m3\handoff.md`

## Working Directory
`c:\IT_System\.agents\teamwork\auditor_m3_1\`

## Requirements & Scope
1. Tiến hành rà soát tĩnh (Static Code Forensics) và kiểm toán thực thi (Execution Validation):
   - Đảm bảo KHÔNG có hardcoded kết quả kiểm thử, dummy responses, mock controllers giả tạo để qua mặt test.
   - Đảm bảo `TicketHub` và `SignalRNotificationDispatcher` thực sự kết nối với WebSocket hub context, không phải là dummy class rỗng.
   - Đảm bảo Controllers trong 5 Areas thực sự tương tác với `ItTmsDbContext`, `ITicketStateFactory`, MediatR/Domain Event Dispatcher, và các Service thật.
   - Đảm bảo client script `signalr.min.js` là file JavaScript hợp lệ của Microsoft SignalR, không phải file rỗng hay stub giả tạo.
   - Đảm bảo 10 kịch bản kiểm nghiệm Bảng 7.2 được cài đặt và kiểm chứng bằng logic thật.
2. Chạy độc lập:
   - `dotnet build c:\IT_System\ItTms.sln`
   - `dotnet test c:\IT_System\ItTms.sln`
3. Đưa ra phán quyết nhị phân dứt khoát:
   - `CLEAN` (nếu hoàn toàn trung thực, không có vi phạm toàn vẹn).
   - `INTEGRITY VIOLATION` (nếu phát hiện gian lận, hardcode, dummy facade - kèm bằng chứng chi tiết).
4. Viết `handoff.md` và gửi thông điệp báo cáo về cho orchestrator.

## 2026-10-25T10:29:10Z
[Message] timestamp=2026-10-25T10:29:10Z sender=9e3cad2c-3e7f-49c0-9814-015216856d2e priority=MESSAGE_PRIORITY_HIGH content=You are auditor_m3_1 (Forensic Auditor). Your working directory is c:\IT_System\.agents\teamwork\auditor_m3_1\.
FIRST: Read c:\IT_System\ORIGINAL_REQUEST.md, c:\IT_System\PROJECT.md, c:\IT_System\.agents\teamwork\auditor_m3_1\DISPATCH.md, and c:\IT_System\.agents\teamwork\worker_m3\handoff.md.
Perform exhaustive forensic integrity verification on Milestone 3:
- Static code analysis: ensure NO hardcoded test results, fake responses, dummy controllers, or mocks bypassing actual logic.
- Verify TicketHub connects to real IHubContext and EF Core DbContext.
- Verify signalr.min.js is the genuine Microsoft SignalR library and not a dummy file.
- Verify 10 Table 7.2 scenarios are implemented authentically.
- Run `dotnet build c:\IT_System\ItTms.sln` and `dotnet test c:\IT_System\ItTms.sln` independently.
Render a BINARY VETO verdict: CLEAN or INTEGRITY VIOLATION.
Write handoff.md with full evidence and verdict. Send completion message back to orchestrator.
