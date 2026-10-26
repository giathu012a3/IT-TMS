# BRIEFING — 2026-10-26T09:15:00Z

## Mission
Khảo sát toàn diện mã nguồn C# .NET 9 ASP.NET Core MVC tại `c:\IT_System` phục vụ lập báo cáo IT-TMS: 8 bảng DbContext/Entities, 4 phân hệ dọc (Staff, Leader, User, Admin/Auth), các Design Patterns, Controllers, Actions, Routes, SignalR Hubs và Services.

## 🔒 My Identity
- Archetype: explorer (teamwork_preview_explorer)
- Roles: survey, analysis, synthesis
- Working directory: c:\IT_System\.agents\teamwork\explorer_survey_1
- Original parent: 3cc6747f-b9cb-4239-907a-6853ec500168
- Milestone: Explorer Survey 1 - Codebase Inventory

## 🔒 Key Constraints
- Read-only investigation — do NOT implement or modify any project code/files
- Ngôn ngữ giao tiếp: Tiếng Việt (Vietnamese) là ngôn ngữ chính, giữ nguyên thuật ngữ kỹ thuật và mã định danh code
- Áp dụng triết lý `#Argon-Omega`: BLUF-MECE, trích dẫn file và dòng chính xác, bằng chứng thực nghiệm cụ thể
- Giao tiếp với Orchestrator qua `send_message` (Recipient: 3cc6747f-b9cb-4239-907a-6853ec500168)

## Current Parent
- Conversation ID: 3cc6747f-b9cb-4239-907a-6853ec500168
- Updated: 2026-10-26T09:15:00Z

## Investigation State
- **Explored paths**:
  - `c:\IT_System\src\ItTms\Data\` (`ItTmsDbContext.cs`, `Configurations/*.cs`, `DataSeeder.cs`, `Interceptors/`)
  - `c:\IT_System\src\ItTms\Models\` (`Entities/*.cs`, `States/*.cs`, `ViewModels/*.cs`, `Events/*.cs`, `Realtime/*.cs`, `Enums/*.cs`, `Common/*.cs`)
  - `c:\IT_System\src\ItTms\Controllers\` (`StaffController.cs`, `LeaderController.cs`, `TicketController.cs`, `AdminController.cs`, `AuthController.cs`, `HomeController.cs`, `BaseController.cs`)
  - `c:\IT_System\src\ItTms\Views\` (`Staff/`, `Leader/`, `Ticket/`, `Admin/`, `Auth/`, `Shared/`)
  - `c:\IT_System\src\ItTms\Hubs\` (`TicketHub.cs`)
  - `c:\IT_System\src\ItTms\Services\` (`SignalRNotificationDispatcher.cs`, `InMemoryDomainEventDispatcher.cs`, `PasswordHasherService.cs`, `Observers/*.cs`)
  - `c:\IT_System\src\ItTms\Filters\` (`AuthorizeRolesAttribute.cs`, `AuthorizeTicketAccessAttribute.cs`)
  - `c:\IT_System\tests\ItTms.Tests\` (342 xUnit tests PASSED 100%)
- **Key findings**:
  - 8 bảng CSDL: Users, TicketStatuses (1..7), Tickets, Comments, Feedbacks, Attachments, Notifications, SystemLogs.
  - 4 phân hệ dọc tương ứng 4 sinh viên:
    - Staff (Nguyễn Gia Thụ): StaffController, State Pattern (7 states: New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected), BOLA TC-07.
    - Leader (Nguyễn Mạnh Tiến): LeaderController, Factory Method / Flyweight Pattern (TicketStateFactory), TC-06 chặn Reject InProgress, BPMN flow.
    - User (Hồ Thanh Tình): TicketController, Observer Pattern (NotificationObserver, SystemLogObserver), SignalR WebSocket (/hubs/ticket), TC-08 chặn comment Resolved/Closed, TC-09 CloseAndRate feedback.
    - Admin & Auth (Trần Thanh Danh): AdminController, AuthController, Decorator Pattern ([AuthorizeRolesAttribute]), Singleton Pattern (PasswordHasherService), TC-01 chặn inactive, TC-02 HTTP 403, TC-10 chặn tự vô hiệu hóa.
- **Unexplored areas**: Không còn (khảo sát hoàn tất 100%).

## Key Decisions Made
- Lập báo cáo kiểm kê chi tiết tại `codebase_inventory.md`.
- Lập báo cáo chuyển giao 5 phần tại `handoff.md`.
- Cập nhật tiến độ liveness tại `progress.md`.

## Artifact Index
- `c:\IT_System\.agents\teamwork\explorer_survey_1\DISPATCH.md` — Lưu vết thông điệp chỉ đạo
- `c:\IT_System\.agents\teamwork\explorer_survey_1\progress.md` — Liveness heartbeat và tiến độ chi tiết
- `c:\IT_System\.agents\teamwork\explorer_survey_1\codebase_inventory.md` — Báo cáo kiểm kê toàn diện mã nguồn
- `c:\IT_System\.agents\teamwork\explorer_survey_1\handoff.md` — Báo cáo chuyển giao 5 phần theo Handoff Protocol
