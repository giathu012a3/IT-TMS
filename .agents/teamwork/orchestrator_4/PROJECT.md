# Project: IT-TMS Document & Diagram Modernization (.NET 9)

## Architecture & System Overview
Hệ thống IT-TMS (IT Ticket Management System) xây dựng trên nền tảng .NET 9 ASP.NET Core MVC kết hợp Entity Framework Core 9 (SQL Server) và SignalR WebSocket thời gian thực.
Hệ thống được thiết kế theo 4 phân hệ dọc độc lập (Vertical Slices):
1. **Phân hệ Kỹ thuật viên (Staff)**: Phụ trách bởi Nguyễn Gia Thụ (25810041) - `StaffController.cs`, `Views/Staff/`, State Pattern (7 trạng thái: New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected).
2. **Phân hệ Trưởng nhóm (Leader)**: Phụ trách bởi Nguyễn Mạnh Tiến (25810043) - `LeaderController.cs`, `Views/Leader/`, Factory Method / Flyweight Pattern (`TicketStateFactory.cs`), quy trình phân công & giám sát SLA.
3. **Phân hệ Người dùng (User)**: Phụ trách bởi Hồ Thanh Tình (25810044) - `TicketController.cs`, `Views/Ticket/`, Observer Pattern & SignalR WebSocket Hub `TicketHub.cs` (`/hubs/ticket`), gửi/nhận thông báo và bình luận realtime.
4. **Phân hệ Quản trị & Bảo mật (Admin & Auth)**: Phụ trách bởi Trần Thanh Danh (25810009) - `AdminController.cs`, `AuthController.cs`, `Views/Admin/`, `Views/Auth/`, Decorator Pattern (`[AuthorizeRoles]`), Singleton Pattern (`PasswordHasherService.cs`).

## Feature & Diagram Inventory
| # | Hạng mục / Sơ đồ | Mô tả kỹ thuật | Milestone | Nguồn / Phân hệ |
|---|------------------|----------------|-----------|-----------------|
| 1 | Bảng PL.1 Báo cáo (R1) | Chuẩn hóa bảng phân công 4 Vertical Slices độc lập, loại bỏ devops/test | M1 | Bảng PL.1 Doc1 & Doc2 |
| 2 | Hình 3.1 (R2) | ERD 8 bảng CSDL (Users, TicketStatuses, Tickets, Comments, Feedbacks, Attachments, Notifications, SystemLogs) | M2 | Trần Thanh Danh (Admin/Auth) |
| 3 | Hình 5.1 (R2) | Sơ đồ BPMN Quy trình xử lý Ticket theo 7 trạng thái chuẩn .NET 9 | M2 | Nguyễn Mạnh Tiến (Leader) |
| 4 | Hình 5.2 (R2) | Sơ đồ State Machine 7 trạng thái vòng đời Ticket | M2 | Nguyễn Gia Thụ (Staff) |
| 5 | Hình 5.3 (R2) | Sơ đồ Class Diagram tổng thể kiến trúc MVC (.NET 9) | M2 | Trần Thanh Danh (Admin/Auth) |
| 6 | Hình 5.6 (R2) | Sơ đồ Sequence UC09: Trưởng nhóm phân công xử lý Ticket | M2 | Nguyễn Mạnh Tiến (Leader) |
| 7 | Hình 6.1 (R2) | Sơ đồ Class Diagram Hiện thực hóa State Pattern (ITicketState, ConcreteStates, TicketStateFactory) | M2 | Nguyễn Gia Thụ (Staff) |
| 8 | Hình 2.1 - 2.5 (R3) | 5 Sơ đồ Use Case chuẩn UML: Tổng thể, Phân hệ User, Leader, Staff, Admin | M3 | Trần Thanh Danh (Toàn hệ thống) |
| 9 | Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12 (R3) | 8 Sơ đồ Sequence chi tiết chuẩn hóa theo C# Controller/Method/SignalR | M3 | 4 Thành viên theo phân hệ |
| 10 | Nhúng ảnh & Định dạng (R1-R3) | Chèn/thay thế 19 ảnh mới vào cả 2 file Docx, dọn dẹp bảng tạm Doc2, căn lề 300 DPI | M4 | Toàn bộ 41 hình ảnh |
| 11 | Kiểm định toàn vẹn & Audit | Validate XML parser, đếm 149 Headings, đối chiếu TOC, Forensic Audit | M5 | Verification & Audit |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|--------------|--------|
| M1 | Chuẩn hóa Bảng PL.1 | Cập nhật Bảng PL.1 ở cả 2 file docx theo 4 vertical slices, giữ 149 headings | Survey | PLANNED |
| M2 | Thiết kế 6 Sơ đồ thiếu (R2) | Viết mã nguồn PlantUML và render 6 ảnh PNG 300 DPI (Hình 3.1, 5.1, 5.2, 5.3, 5.6, 6.1) | Survey | PLANNED |
| M3 | Hiện đại hóa 13 Sơ đồ cũ (R3) | Viết mã nguồn PlantUML và render 13 ảnh PNG 300 DPI (Hình 2.1-2.5, Sequence 5.4-5.12) | Survey | PLANNED |
| M4 | Nhúng ảnh & Format Word | Thay thế 13 ảnh cũ + chèn 6 ảnh mới vào cả 2 file docx, dọn dẹp bảng tạm Doc2 | M1, M2, M3 | PLANNED |
| M5 | Verification & Forensic Audit | Validate XML, đếm 149 headings, verify 41/41 ảnh, đối chiếu TOC, Forensic Audit | M4 | PLANNED |

## Code & File Layout
- Mã nguồn sơ đồ PlantUML & ảnh render: `c:\IT_System\docs\diagrams\`
- Script tự động hóa docx: `c:\IT_System\scripts\`
- Tài liệu Word gốc: `c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx`
- Tài liệu Word .NET 9: `c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx`
- Backup tài liệu Word: `c:\IT_System\backups\`
