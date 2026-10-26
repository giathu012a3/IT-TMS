# 📌 PROJECT MEMORY: IT-TMS (.NET 9 Enterprise Clean Architecture)
> Last Updated: 2026-10-25 | Phase: Engineering & Architecture Overhaul | Language: C# (.NET 9)

## 1. Core Stack & Architectural Invariants
- **Project**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)
- **Team**: Nhóm 7 (Nguyễn Gia Thụ - 25810041, Nguyễn Mạnh Tiến - 25810043, Hồ Thanh Tình - 25810044, Trần Thanh Danh - 25810009)
- **Course**: Thiết kế Phần mềm Hướng Đối tượng (TKPMHDT) - HCMUTE
- **Stack**:
  - **Framework**: .NET 9 (`net9.0`)
  - **Architecture**: Pragmatic Clean Architecture + Vertical Slices (`dotnet-clean-architecture`)
  - **Presentation**: ASP.NET Core MVC (5 Areas: `Auth`, `User`, `Staff`, `Leader`, `Admin`) + Razor Views + Tailwind/Modern UI
  - **Real-Time Communication**: **SignalR WebSocket (`TicketHub`)** (Thay thế triệt để anti-pattern Polling 3s)
  - **Persistence**: EF Core 9 (SQLite / SQL Server LocalDB), DbContext Pooling, Compiled Queries, Keyset Pagination, Concurrency Token (`RowVersion`)
  - **Security**: Cookie Authentication + Claims-Based RBAC (`AuthorizeRolesAttribute`), `IPasswordHasher<User>`, Anti-CSRF
  - **Design Patterns**: State Pattern (7 states), Observer Pattern (Domain Events -> Notification/SystemLog), Decorator/Filter (RBAC), Singleton (DI), MVC Areas.
  - **Testing**: xUnit + FluentAssertions + Moq.

## 2. Hard Invariants (DO NOT VIOLATE)
1. **Kill the 3-Second Polling Anti-Pattern (`#026 Bottleneck`)**:
   - Khung chat/trao đổi và chuông thông báo phải sử dụng **ASP.NET Core SignalR WebSocket** hai chiều. Tuyệt đối không dùng client setInterval polling 3s gây nghẽn I/O và thắt cổ chai database.
2. **Railway-Oriented `Result<T>` (`#026`)**:
   - Không ném exception cho các vi phạm nghiệp vụ thông thường (sai trạng thái, thiếu quyền, validate thất bại). Dùng `Result<T>` và `Error`.
3. **Rich Domain Model & State Encapsulation (`#001 First-Principles`)**:
   - Vòng đời Ticket được điều phối nghiêm ngặt qua 7 State classes kế thừa `TicketStateBase`. Mọi chuyển trạng thái không hợp lệ bị chặn ngay tại Domain Model.
4. **Idempotency & Concurrency Safety (`#035`)**:
   - Sử dụng Concurrency Token (`RowVersion`) để ngăn chặn Lost Updates khi hai kỹ thuật viên cùng thao tác trên một ticket.
5. **Preserve Report TOC Layout 100%**:
   - Trong quá trình đồng bộ hóa nội dung báo cáo Word (`BaoCao_ITTMS_TKPMHDT (4).docx`), tuyệt đối không thay đổi thứ tự và tiêu đề các chương mục (từ Chương 1 đến Chương 7). Chỉ nâng cấp nội dung công nghệ, bảng biểu CSDL và mã nguồn sang C# / .NET 9.

## 3. Active Architectural Decisions (One-Way Doors)
- **ADR-001 (Real-Time SignalR WebSocket Migration)**: Thay thế hoàn toàn cơ chế Polling 3s bằng SignalR Hub (`/hubs/ticket`). Khi có comment mới hoặc cập nhật trạng thái, server đẩy trực tiếp xuống client qua SignalR Group `ticket_{id}`.
- **ADR-002 (Clean Architecture + Vertical Slices)**: Phân tách giải pháp thành 5 projects: `ItTms.Domain`, `ItTms.Application`, `ItTms.Infrastructure`, `ItTms.Web`, `ItTms.UnitTests`.
- **ADR-003 (Enterprise Database Schema)**: Bổ sung Concurrency Token, SLA DueAt, Metadata tệp đính kèm và chỉ mục bao phủ (Covering Indexes) vào 8 bảng cốt lõi.

## 4. Rolling Worklog
- **[2026-10-26 - Phase 3]**:
  - Chụp tự động thực tế và thay thế trọn vẹn toàn bộ 22 hình ảnh giao diện Chương 4 (`Hình 4.1` đến `Hình 4.22`) trong cả hai tài liệu Word (`BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`) từ ứng dụng .NET 9 ASP.NET Core MVC đang chạy live trên `http://localhost:5050`. Tỉ lệ chuẩn 16:10, căn lề giữa, độ phân giải cao, bảo toàn tuyệt đối 149/149 tiêu đề và thứ tự mục lục (TOC).
  - Dọn dẹp triệt để các file rác, file script thử nghiệm tạm thời, file database thừa ở thư mục gốc và thư mục trùng lặp, giữ cấu trúc workspace chuẩn mực và sạch sẽ.
  - Tối giản hóa và phẳng hóa toàn bộ kiến trúc hệ thống về **1 Project ASP.NET Core MVC kinh điển duy nhất (`src/ItTms`)** theo cấu trúc thư mục tiêu chuẩn (`Controllers/`, `Models/`, `Views/`, `Data/`, `Services/`, `Hubs/`, `Filters/`, `wwwroot/`, `Program.cs`, `appsettings.json`, `ItTms.csproj`).
  - Bảo toàn 100% các mẫu thiết kế GoF: State Pattern (7 states), Observer Pattern (2-phase Domain Event Dispatcher), Decorator Pattern (`[AuthorizeRoles]`), Singleton, Flyweight Factory, và Real-time SignalR WebSocket (`/hubs/ticket`).
  - Kiểm thử tự động trên Google Chrome thực tế qua Selenium Walkthrough: 16/16 kịch bản đều đạt trạng thái `[PASS]` 100%.
  - Đồng bộ hóa toàn diện nội dung Mục 6.6, 7.1, 7.2 và các Bảng 56, 58 trong cả 2 tài liệu báo cáo Word (`BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`), bảo toàn 100% thứ tự mục lục (TOC) và tiêu đề.
  - Triển khai toàn diện R1: Khắc phục triệt để lỗi Chat Realtime SignalR & phím tắt Enter (bắt `keydown` với `!e.isComposing`, optimistic display, `data-comment-id` deduplication, `CommentAddedEvent` 2-phase commit, phân tách bảo mật 4 lớp cho `IsInternal`).
  - Triển khai toàn diện R2: Đồng bộ bảng màu Enterprise Neutral Light & Carbon Dark trên toàn bộ 23 Razor Views (xóa sạch `text-primary`/`text-danger` trên `h1`–`h6`, mã `#ID` monospace trung tính, Low/Med pill `bg-secondary-subtle`, KPI neutral, WCAG AA >= 4.5:1).
  - Triển khai toàn diện R3: Hệ thống Floating Card Box đa tầng góc trên bên phải (`site.css`, `_ToastAlerts.cshtml`, `_SvgIcons.cshtml`, `signalr-notification.js`), thanh tiến trình 5s hover pause, 100% SVG, loại bỏ `alert()` và bảo toàn layout 2 cột `Login.cshtml`.
  - Triển khai toàn diện R4: Nâng tổng số bài test lên 342 xUnit tests (pass 100%), bảo toàn trọn vẹn 10 kịch bản Bảng 7.2, vượt qua thẩm định đối kháng và giám định pháp y Forensic Integrity Audit (CLEAN).

## 5. Immediate Next Tasks (Priority Queue)
- [x] **P0**: Khởi tạo Solution .NET 9 (`ItTms.sln`) và 5 dự án thành phần (`Domain`, `Application`, `Infrastructure`, `Web`, `Tests`).
- [x] **P0**: Cài đặt Domain Entities, State Pattern (7 trạng thái + Flyweight Factory) và Observer Domain Events.
- [x] **P0**: Cài đặt Infrastructure (EF Core `ItTmsDbContext` SQLite WAL mode, Migrations, Seed Data, PBKDF2 Security).
- [x] **P0**: Cài đặt SignalR `TicketHub` (`/hubs/ticket`) và Controllers/Views trong 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`).
- [x] **P1**: Viết bộ xUnit Tests (336 tests passed 100%), nghiệm thu trọn vẹn 10 kịch bản trong Bảng 7.2 (`Table72VerificationTests`).
- [x] **P1**: Đồng bộ hóa file báo cáo Word (`BaoCao_ITTMS_TKPMHDT (4).docx`), cập nhật nội dung C# / .NET 9 / SignalR và điền kết quả kiểm thử thực tế, giữ nguyên 100% (373/373) mục lục và tiêu đề.
- [x] **P0 (Phase 2)**: Khắc phục lỗi Khung Chat Realtime SignalR, phím tắt Enter, hiển thị tức thì & deduplication (R1).
- [x] **P0 (Phase 2)**: Đồng bộ bảng màu Enterprise Neutral Light & Carbon Dark trên 23 Views, WCAG AA >= 4.5:1 (R2).
- [x] **P0 (Phase 2)**: Thiết kế lại hệ thống Floating Card Box nhiều tầng cố định top-right, hover-pause 5s, 100% SVG (R3).
- [x] **P0 (Phase 2)**: Bảo toàn 100% nghiệp vụ 20 Use Cases & 342 tests PASS, hoàn tất Gate Review, Adversarial Challenge và Forensic Audit CLEAN (R4).
