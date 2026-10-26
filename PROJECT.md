# Project: IT Ticket Management System (.NET 9) — IT-TMS

## Architecture
- **Paradigm**: Pragmatic Clean Architecture with 5 Areas ASP.NET Core MVC and SignalR WebSockets.
- **Target Framework**: .NET 9 (`net9.0`), C# 13, ASP.NET Core MVC.
- **Database**: SQLite with Entity Framework Core 9, Optimistic Concurrency Tokens (`ConcurrencyToken`), Covering Indexes, and automated seeding.
- **Solution Layout**:
  - `src/ItTms.Domain`: 8 Core Entities, 7 Ticket States (State Pattern with `Result<T>`), Domain Events, Value Objects.
  - `src/ItTms.Application`: Service interfaces, DTOs, CQRS/Use Case Handlers, Observers (`NotificationObserver`, `SystemLogObserver`).
  - `src/ItTms.Infrastructure`: `ItTmsDbContext`, EF Core 9 SQLite configurations, migrations, Data Seeder, PasswordHasher, SignalR Notification Dispatcher.
  - `src/ItTms.Web`: 5 MVC Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`), `TicketHub` SignalR endpoint, `AuthorizeRolesAttribute`, ViewModels, Bootstrap 5 UI.
  - `tests/ItTms.Tests`: Comprehensive test suite containing State Machine unit tests, Concurrency tests, SignalR integration tests, and 10 Table 7.2 verification scenarios.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | UC01 - Đăng nhập hệ thống & Floating Card Error | Xác thực tài khoản bằng Email/Password, hiển thị lỗi qua Floating Card không xô lệch bố cục 2 cột | M2, M4 | R2, R3, Table 7.2 |
| 2 | UC02 - Đăng xuất & ForceLogout Floating Card | Hủy bỏ Cookie session an toàn; sự kiện ForceLogout qua SignalR dùng Floating Card Box thay vì alert | M2 | R3, UC02 |
| 3 | UC03 - Đổi mật khẩu | Xác thực mật khẩu cũ và cập nhật mật khẩu mới, giao diện chuẩn bảng màu Carbon | M4 | R2, UC03 |
| 4 | UC04 - Tạo ticket mới | User tạo yêu cầu hỗ trợ, tự động tính SLA DueAt, trạng thái New, thông báo Leader qua Floating Card | M2, M4 | R2, R3, UC04 |
| 5 | UC05 - Xem danh sách ticket cá nhân | User lọc, tìm kiếm và xem danh sách ticket; cột #ID monospace trung tính | M4 | R2, UC05 |
| 6 | UC06 - Xem chi tiết & theo dõi tiến độ | User xem tiến trình xử lý ticket, danh sách đính kèm và lịch sử; loại bỏ text-primary trên header | M4 | R2, UC06 |
| 7 | UC07 - Thêm bình luận vào ticket (Realtime SignalR & Enter) | Enter gửi ngay, Shift+Enter xuống dòng; CommentAddedEvent phát SignalR; hiển thị tức thì + deduplication | M1, M3 | R1, UC07 |
| 8 | UC08 - Tải tệp đính kèm | Upload tệp hỗ trợ kèm metadata dung lượng, MIME type | M4 | UC08 |
| 9 | UC09 - Xác nhận đóng & Đánh giá ticket | User đóng ticket đã Resolved và đánh giá chất lượng; modal header trung tính | M4 | R2, UC09 |
| 10 | UC10 - Tiếp nhận & Bắt đầu xử lý ticket | Staff chuyển trạng thái ticket từ Assigned sang InProgress | M4 | UC10 |
| 11 | UC11 - Tạm dừng chờ thông tin | Staff chuyển ticket sang Waiting khi cần thêm thông tin từ User | M4 | UC11 |
| 12 | UC12 - Hoàn thành xử lý ticket | Staff chuyển trạng thái ticket sang Resolved, ghi nhận ResolvedAt | M4 | UC12 |
| 13 | UC13 - Xem danh sách ticket phân công | Staff xem danh sách ticket phân công; chặn xem ticket người khác | M4 | R2, UC13 |
| 14 | UC14 - Phân công ticket cho Staff | Leader phân công ticket New cho kỹ thuật viên; modal header trung tính | M4 | R2, UC14 |
| 15 | UC15 - Từ chối tiếp nhận ticket | Leader từ chối ticket không hợp lệ; modal header trung tính | M4 | R2, UC15 |
| 16 | UC16 - Điều chuyển ticket giữa các Staff | Leader tái phân công ticket; modal header trung tính | M4 | R2, UC16 |
| 17 | UC17 - Dashboard thống kê & SLA | 4 Dashboard đồng nhất icon KPI trung tính, pill Low/Medium trung tính | M4 | R2, UC17 |
| 18 | UC18 - Quản lý tài khoản người dùng | Admin quản lý tài khoản; header modal và form trung tính | M4 | R2, UC18 |
| 19 | UC19 - Phân quyền vai trò | Admin quản trị vai trò, chặn truy cập trái quyền (403 Forbidden) | M4 | R2, UC19 |
| 20 | UC20 - Xem nhật ký hệ thống | Admin kiểm tra SystemLogs; lưu log khi có CommentAddedEvent | M1, M4 | R1, UC20 |
| 21 | R1.1 - Shortcut phím Enter ô Chat | Enter gửi tin nhắn tức thì (hỗ trợ IME Telex), Shift+Enter xuống dòng | M3 | R1 |
| 22 | R1.2 - Backend CommentAddedEvent & SignalR Broadcast | AddComment ở User/Staff/Leader phát CommentAddedEvent, trả về CommentDto, gửi chuông và log | M1 | R1 |
| 23 | R1.3 - Optimistic UI & Deduplication | Hiển thị tức thì bên người gửi, nhận tức thì bên người nhận, chống trùng lặp theo data-comment-id | M3 | R1 |
| 24 | R1.4 - Bảo mật Ghi chú Nội bộ (IsInternal) | Ghi chú nội bộ chỉ phát cho Staff/Leader/Admin, tuyệt đối không gửi sang User | M1, M3 | R1 |
| 25 | R2.1 - Loại bỏ màu cứng trên 23 Views | Xóa bỏ text-primary/text-danger trên h1-h6 và modal-title ở 23 views | M4 | R2 |
| 26 | R2.2 - Chuẩn hóa cột mã #ID | Cột mã #ID dùng monospace trung tính (font-monospace text-muted) | M4 | R2 |
| 27 | R2.3 - Chuẩn hóa Badge Mức ưu tiên | Priority Low & Medium dùng pill trung tính bg-secondary-subtle; sửa lỗi Staff Dashboard | M4 | R2 |
| 28 | R2.4 - Đồng nhất KPI Card Dashboards | Chuẩn hóa icon và màu sắc KPI trên 4 Dashboard về tông trung tính | M4 | R2 |
| 29 | R3.1 - Floating Card Box đa tầng | Thiết kế Floating Card Box neo cố định top-right, viền màu phân loại, shadow-float | M2 | R3 |
| 30 | R3.2 - Thanh tiến trình đếm ngược 5s | Auto-dismiss progress bar 5s có hover pause | M2 | R3 |
| 31 | R3.3 - Triệt tiêu alert() & Icon SVG | Loại bỏ toàn bộ alert() và <i class="bi ...">, dùng 100% SVG sprite/inline | M2, M3 | R3 |
| 32 | R3.4 - Login Floating Card Alert | Thông báo lỗi Login dạng Floating Card không làm xô lệch layout .auth-split-shell | M2, M4 | R3 |
| 33 | R4.1 - Bảo toàn 336 xUnit Tests & Table 7.2 | Đảm bảo 100% tests hiện có tiếp tục PASS | M5 | R4 |
| 34 | R4.2 - Bổ sung Unit/Integration Tests | Viết unit/integration tests cho luồng CommentAddedEvent & SignalR | M1, M5 | R4 |
| 35 | R4.3 - Kiểm thử E2E Browser & Audit | Kiểm thử tương tác thực tế trên browser, kiểm tra WCAG AA >= 4.5:1, Forensic Audit sạch | M5 | R4 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | M1 - Backend Realtime Engine & Events | Gắn CommentAddedEvent trong AddComment (User, Staff, Leader), trả CommentDto, cập nhật SignalRNotificationDispatcher & tests | none | DONE |
| 2 | M2 - Multi-tier Floating Card Box Notification System | CSS .floating-card-box, _ToastAlerts.cshtml top-right, _SvgIcons.cshtml, signalr-notification.js, Login alert | none | DONE |
| 3 | M3 - Frontend Realtime Chat & Enter Shortcut | ticket-realtime.js: keydown Enter/Shift+Enter, optimistic render, data-comment-id deduplication, remove alert() & bi-icons | M1, M2 | DONE |
| 4 | M4 - 23 Views Palette & KPI Synchronization | Rà soát 23 views: xóa text-primary/danger trên headers, #ID monospace, Low/Med bg-secondary-subtle, KPI neutral | none | DONE |
| 5 | M5 - Testing, E2E Verification & Forensic Audit | Chạy 336 tests + new tests (342/342 pass), E2E browser tests, contrast check, Challenger & Forensic Auditor verification | M1, M2, M3, M4 | DONE |

## Interface Contracts
### `AddComment` Backend ↔ Frontend Ajax
- **Request**: `POST /Area/Controller/AddComment` (Form data / Ajax)
  - `ticketId`: `int`
  - `content`: `string`
  - `isInternal`: `bool` (Staff/Leader only; User forced to false)
- **Response**: `application/json`
  - `success`: `bool`
  - `message`: `string`
  - `comment`: `CommentDto`
    - `id`: `int`
    - `ticketId`: `int`
    - `authorId`: `int`
    - `authorName`: `string`
    - `authorRole`: `string`
    - `content`: `string`
    - `createdAt`: `DateTime` / `ISO string`
    - `isInternal`: `bool`
- **Domain Event**: `CommentAddedEvent(Ticket ticket, Comment comment, User author)`
  - Handled by `SignalRNotificationDispatcher.HandleCommentAddedAsync` -> pushes `ReceiveComment` to SignalR group `ticket_{id}` (or `ticket_{id}_internal`) and `ReceiveNotification` to recipient users.
  - Handled by `NotificationObserver` -> writes Notification entity.
  - Handled by `SystemLogObserver` -> writes SystemLog entity.

### Floating Card Box Notification Interface
- **Global JS API**:
  - `window.showToastCard(type, title, message, durationMs)`
  - `type`: `'success' | 'danger' | 'warning' | 'info'`
  - Container element: `<div id="toast-container" class="floating-notification-stack position-fixed top-0 end-0 p-3" style="z-index: 1090;"></div>`
  - Structure:
    - `.floating-card-box` with semantic left border (`border-start-4`), `shadow-float`, flex layout
    - Icon container with inline SVG or `<svg class="bi"><use href="#..."></use></svg>`
    - Text body: `.fw-bold` category title, `.text-secondary` description
    - Close button (`btn-close`)
    - Progress bar: `.floating-card-progress` animating 0% -> 100% width over `durationMs` (default 5000ms), paused on `mouseenter`, resumed on `mouseleave`.

## Code Layout
```
c:\IT_System\
├── src\
│   ├── ItTms.Domain\
│   │   ├── Entities\ (User, Ticket, TicketStatus, Comment, Feedback, Attachment, Notification, SystemLog)
│   │   ├── Events\ (CommentAddedEvent, etc.)
│   │   └── States\ (7 Ticket States)
│   ├── ItTms.Application\
│   │   ├── Features\Comments\Dtos\ (CommentDto)
│   │   └── Observers\ (NotificationObserver, SystemLogObserver)
│   ├── ItTms.Infrastructure\
│   │   ├── Persistence\ (ItTmsDbContext)
│   │   └── Realtime\ (SignalRNotificationDispatcher)
│   └── ItTms.Web\
│       ├── Areas\ (Auth, User, Staff, Leader, Admin)
│       ├── Views\Shared\ (_Layout, _ToastAlerts, _SvgIcons, _StatusBadge, _SlaBadge)
│       └── wwwroot\ (css/site.css, js/ticket-realtime.js, js/signalr-notification.js)
├── tests\
│   └── ItTms.Tests\
│       ├── UnitTests\
│       └── E2EVerificationTests\ (Table72VerificationTests)
└── PROJECT.md
```
