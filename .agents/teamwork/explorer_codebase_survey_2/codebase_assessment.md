# 📋 BÁO CÁO KHẢO SÁT HIỆN TRẠNG MÃ NGUỒN VÀ KIẾN TRÚC IT-TMS
**Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
**Nền tảng mục tiêu**: .NET 9 (`net9.0`) | C# 13 | ASP.NET Core MVC | Clean Architecture | SignalR WebSockets | EF Core 9 (SQLite)  
**Đơn vị thực hiện**: `explorer_codebase_survey_2`  
**Ngày thực hiện**: 2026-10-25  
**Phương pháp luận**: Chuẩn nhận thức `#Argon-Omega` (BLUF-MECE, First-Principles, Causal Constructivism)  

---

## 1. TÓM TẮT ĐIỀU HÀNH (EXECUTIVE SUMMARY - BLUF)

1. **Hiện trạng mã nguồn (As-Is)**:
   - Thư mục làm việc `c:\IT_System` đang ở trạng thái **Greenfield hoàn toàn**: chưa có bất kỳ file giải pháp (`.sln`), file dự án (`.csproj`), file mã nguồn C# (`.cs`), migrations hay file cơ sở dữ liệu SQLite (`*.db`) nào.
   - Hiện chỉ có các tài liệu dự án:
     - `AGENTS.md` (chỉ thị quy tắc của hệ thống agent).
     - `PROJECT_MEMORY.md` (bộ nhớ dự án và các quyết định kiến trúc cốt lõi).
     - `BaoCao_ITTMS_TKPMHDT (4).docx` (file báo cáo đồ án nguyên bản bằng Python/Flask).
     - `extracted_doc_text.txt` (nội dung trích xuất thô từ file báo cáo docx).
     - Thư mục `.agents/` chứa bộ quy tắc và các kỹ năng kiến trúc chuyên sâu.
2. **Hiện trạng môi trường máy chủ**:
   - Hệ điều hành: Windows 10/11 Enterprise x64 (Build 10.0.26200).
   - .NET SDK: **`9.0.317`** (C:\Program Files\dotnet\sdk\9.0.317\).
   - .NET Runtimes: `Microsoft.AspNetCore.App 9.0.19`, `Microsoft.NETCore.App 9.0.19`.
   - Kết luận môi trường: Đầy đủ và hoàn hảo 100% để khởi tạo, biên dịch và chạy giải pháp .NET 9 nguyên bản mà không cần cài đặt thêm runtime bên ngoài.
3. **Mục tiêu chuyển đổi (To-Be)**:
   - Xây dựng từ đầu hệ thống IT-TMS theo **Pragmatic Clean Architecture + Vertical Slices** gồm 5 projects (`ItTms.Domain`, `ItTms.Application`, `ItTms.Infrastructure`, `ItTms.Web`, `ItTms.UnitTests`).
   - Cung cấp giao diện ASP.NET Core MVC với 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) trên nền Bootstrap 5, bao phủ trọn vẹn 20 Use Cases.
   - Xóa bỏ triệt để anti-pattern Polling 3s của đồ án cũ, thay bằng **SignalR WebSocket (`TicketHub`)** cho khung trao đổi comment và chuông thông báo.
   - Hiện thực hóa 4 Design Patterns: State Pattern (7 states với `Result<T>`), Observer Pattern (Domain Events -> Notifications & SystemLogs), Decorator Pattern (`AuthorizeRolesAttribute`), Singleton Pattern (DI Services).
   - Vượt qua 100% 10 kịch bản kiểm thử nghiệm thu trong Bảng 7.2 của báo cáo.

---

## 2. KHẢO SÁT CẤU TRÚC SOLUTION VÀ CÁC DỰ ÁN (.NET 9 CLEAN ARCHITECTURE)

### 2.1. Hiện trạng thực tế
- Lệnh tìm kiếm giải pháp và dự án:
  - `find_by_name(Pattern="*.sln")` -> Trả về 0 kết quả.
  - `find_by_name(Pattern="*.csproj")` -> Trả về 0 kết quả.
  - `find_by_name(Pattern="*.cs")` -> Trả về 0 kết quả.
- Kết luận: Chưa có project nào được scaffold trên đĩa.

### 2.2. Đề xuất Kiến trúc Mục tiêu (Pragmatic Clean Architecture)
Căn cứ theo kỹ năng `.agents/skills/backend/dotnet-clean-architecture`, giải pháp được tổ chức thành 5 dự án cấu trúc rõ ràng:

```text
c:\IT_System\
├── ItTms.sln
├── src/
│   ├── ItTms.Domain/                 # [net9.0] Class Library (Zero external dependencies)
│   │   ├── Common/                   # BaseEntity, AggregateRoot, IDomainEvent, Result<T>, Error
│   │   ├── Entities/                 # User, Ticket, TicketStatus, Comment, Feedback, Attachment, Notification, SystemLog
│   │   ├── Enums/                    # UserRole, TicketPriority, TicketCategory
│   │   ├── Events/                   # TicketCreatedEvent, TicketAssignedEvent, TicketStatusChangedEvent...
│   │   └── States/                   # State Pattern: TicketStateBase + 7 concrete states
│   │
│   ├── ItTms.Application/            # [net9.0] Class Library
│   │   ├── Common/Interfaces/        # IApplicationDbContext, ICurrentUser, IPasswordHasherService, IDateTimeProvider
│   │   ├── DTOs/                     # TicketDto, UserDto, CommentDto, NotificationDto, FeedbackDto...
│   │   ├── Features/                 # Vertical slices theo nghiệp vụ (Tickets, Users, Comments, Notifications)
│   │   └── Observers/                # Observer interfaces & domain event handlers
│   │
│   ├── ItTms.Infrastructure/         # [net9.0] Class Library
│   │   ├── Persistence/              # ApplicationDbContext, EntityTypeConfigurations, Migrations
│   │   ├── Seed/                     # DatabaseSeeder (Default roles, statuses, test users for 4 roles)
│   │   ├── Services/                 # PasswordHasher, DateTimeProvider, FileStorageService
│   │   └── Observers/                # NotificationObserver, SystemLogObserver implementations
│   │
│   └── ItTms.Web/                    # [net9.0] ASP.NET Core MVC + SignalR
│       ├── Areas/
│       │   ├── Auth/Controllers/     # AccountController (Login, Logout, ChangePassword, AccessDenied)
│       │   ├── User/Controllers/     # Dashboard, Tickets (MyTickets, Create, Detail, Comment, Feedback)
│       │   ├── Staff/Controllers/    # Dashboard, WorkQueue, UpdateStatus, Comment, History
│       │   ├── Leader/Controllers/   # Dashboard, AllTickets, Assign, Reject, SelfAssign, Comment
│       │   └── Admin/Controllers/    # Dashboard, Users (Create, Toggle, ResetPassword), SystemLogs
│       ├── Hubs/                     # TicketHub (SignalR WebSocket bidirectional channel)
│       ├── Filters/                  # AuthorizeRolesAttribute (Decorator / ActionFilter)
│       ├── Views/                    # Shared Layouts (_Layout.cshtml, _Sidebar.cshtml, _Navbar.cshtml) + Area Views
│       ├── wwwroot/                  # Bootstrap 5 bundle, css, js (ticketHub.js, toastr/alerts)
│       └── Program.cs                # Dependency Injection, Middleware pipeline, Hub routing
│
└── tests/
    └── ItTms.UnitTests/              # [net9.0] xUnit + FluentAssertions + Moq
        ├── Domain/                   # Pure State Machine transition & Result<T> tests
        ├── Security/                 # AuthorizeRolesAttribute & Inactive Account tests
        └── Scenarios/                # Automated verification for Table 7.2 (10 Core Scenarios)
```

### 2.3. Quy tắc phụ thuộc (Dependency Invariants)
- `ItTms.Domain` không phụ thuộc vào bất kỳ framework hoặc ORM nào (kể cả EF Core).
- `ItTms.Application` chỉ phụ thuộc vào `ItTms.Domain`.
- `ItTms.Infrastructure` phụ thuộc vào `ItTms.Application` và `ItTms.Domain`, chịu trách nhiệm hiện thực hóa kết nối CSDL và các dịch vụ ngoại vi.
- `ItTms.Web` là host project, phụ thuộc `ItTms.Application` và `ItTms.Infrastructure`.
- Không sử dụng `IRepository<T>` bọc ngoài EF Core theo chuẩn `#042 Leaky Abstraction`. Sử dụng trực tiếp `IApplicationDbContext` trong tầng Application.

---

## 3. KHẢO SÁT MÔ HÌNH DỮ LIỆU, EF CORE DBCONTEXT & SQLITE

### 3.1. Hiện trạng
- Chưa có file database `it_tms.db` hoặc bất kỳ script SQLite nào trên đĩa.
- Báo cáo gốc mô tả 8 bảng nhưng cấu trúc dạng thô (Flask/SQLAlchemy), thiếu Concurrency Token, thiếu SLA DueAt và chưa có chỉ mục bao phủ (Covering Indexes).

### 3.2. Đặc tả 8 Thực thể Cốt lõi (Enterprise Schema)
Áp dụng kỹ năng `efcore-database-performance`, 8 thực thể được tái thiết kế chuẩn hóa:

| STT | Thực thể | Khóa chính | Thuộc tính quan trọng | Ràng buộc & Tối ưu |
| :--- | :--- | :--- | :--- | :--- |
| 1 | **Users** | `int Id` | `Username`, `PasswordHash`, `FullName`, `Email`, `Phone`, `Role` (Enum), `IsActive`, `CreatedAt`, `UpdatedAt` | Unique index trên `Username` & `Email`. Query filter hoặc check `IsActive`. |
| 2 | **TicketStatuses** | `int Id` | `Code` (New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected), `Name`, `Description`, `BadgeColor` | Bảng danh mục tĩnh, seed 7 trạng thái cố định. |
| 3 | **Tickets** | `int Id` | `Title`, `Description`, `Priority` (Low, Medium, High, Urgent), `Category`, `StatusId`, `RequesterId`, `AssigneeId` (nullable), `DueAt` (SLA), `ResolvedAt`, `ClosedAt`, `CreatedAt`, `UpdatedAt`, `RowVersion` | Concurrency Token (`RowVersion`) chống Lost Updates; Covering Indexes cho truy vấn hàng đợi. |
| 4 | **Comments** | `int Id` | `TicketId`, `UserId`, `Content`, `IsInternal`, `CreatedAt` | FK tới Ticket và User; Index trên `(TicketId, CreatedAt)`. Chặn comment khi Ticket ở trạng thái `Resolved`. |
| 5 | **Feedbacks** | `int Id` | `TicketId` (1-1 với Ticket), `Rating` (1-5 sao), `Comment`, `CreatedAt` | Bắt buộc liên kết với Ticket đã `Resolved` để hoàn tất chuyển sang `Closed`. |
| 6 | **Attachments** | `int Id` | `TicketId`, `FileName`, `FilePath`, `FileSize`, `ContentType`, `UploadedById`, `UploadedAt` | Lưu trữ metadata tệp đính kèm; kiểm tra dung lượng và đuôi tệp an toàn (OWASP). |
| 7 | **Notifications**| `int Id` | `UserId`, `Title`, `Message`, `Type`, `TargetUrl`, `IsRead`, `CreatedAt` | Index `(UserId, IsRead, CreatedAt)`; phục vụ chuông thông báo Real-time SignalR. |
| 8 | **SystemLogs** | `int Id` | `UserId` (nullable), `Action`, `EntityName`, `EntityId`, `Details`, `IpAddress`, `Timestamp` | Audit trail bất biến (Append-only); do `SystemLogObserver` ghi lại khi có biến động. |

### 3.3. Các cơ chế EF Core 9 chuyên sâu bắt buộc
1. **Optimistic Concurrency Control (`#035 Idempotency`)**:
   - Trường `byte[] RowVersion` trên thực thể `Ticket`.
   - Cấu hình qua Fluent API: `builder.Property(t => t.RowVersion).IsRowVersion();`.
   - Ngăn chặn triệt để tình trạng hai kỹ thuật viên hoặc Leader cùng cập nhật một ticket cùng một thời điểm gây ghi đè dữ liệu (Lost Update).
2. **Covering Indexes (`efcore-database-performance`)**:
   - `IX_Tickets_Requester_Status`: `(RequesterId, StatusId)` kèm `CreatedAt` cho trang My Tickets.
   - `IX_Tickets_Assignee_Status`: `(AssigneeId, StatusId)` cho Work Queue của Staff.
   - `IX_Comments_Ticket_Created`: `(TicketId, CreatedAt)` phục vụ tải lịch sử trao đổi tức thì.
   - `IX_Notifications_User_Unread`: `(UserId, IsRead, CreatedAt)` phục vụ đếm badge chưa đọc.
3. **SLA Tracking Timestamps**:
   - Tính toán tự động thời hạn xử lý `DueAt` khi tạo ticket dựa trên `Priority`:
     - `Urgent`: 4 giờ
     - `High`: 8 giờ
     - `Medium`: 24 giờ
     - `Low`: 72 giờ
   - Cập nhật `ResolvedAt` khi ticket chuyển sang `Resolved`, `ClosedAt` khi chuyển sang `Closed`.
4. **Tự động Khởi tạo & Seed Dữ liệu (Auto-migration & Seeding)**:
   - Tự động gọi `context.Database.Migrate()` khi khởi động ứng dụng.
   - Seed đủ 4 nhóm tài khoản mặc định và 1 tài khoản bị vô hiệu hóa để phục vụ nghiệm thu:
     - Admin: `admin` / `Admin@123`
     - Leader: `leader` / `Leader@123`
     - Staff 1: `staff1` / `Staff@123`, Staff 2: `staff2` / `Staff@123`
     - User 1: `user1` / `User@123`, User 2: `user2` / `User@123`
     - Inactive User: `inactive_user` / `User@123` (`IsActive = false`)

---

## 4. KHẢO SÁT CONTROLLERS, VIEWS, 5 AREAS VÀ UI BOOTSTRAP 5

### 4.1. Hiện trạng
- Chưa có file Controller hay View nào.
- Báo cáo cũ sử dụng Jinja2 + Tailwind CSS (Flask), tổ chức Blueprint phân tán. Cần chuyển đổi thành ASP.NET Core MVC Areas chuẩn mực với Razor View Engine và Bootstrap 5.

### 4.2. Thiết kế 5 Phân vùng Nghiệp vụ (5 MVC Areas)

#### A. Area `Auth`
- **Controller**: `AccountController`
- **Use Cases**: UC01 (Đăng nhập), UC05 (Đăng xuất), UC17 (Đổi mật khẩu cá nhân), Báo lỗi 403 (Forbidden).
- **Quy tắc**:
  - Đăng nhập kiểm tra cả thông tin xác thực lẫn cờ `IsActive`. Nếu `IsActive == false`, từ chối ngay lập tức với thông báo `"Tài khoản đã bị vô hiệu hóa"` (Kịch bản 1).
  - Sử dụng ASP.NET Core Cookie Authentication với Claims: `NameIdentifier`, `Name`, `Role`, `Email`.

#### B. Area `User`
- **Controllers**: `DashboardController`, `TicketController`
- **Use Cases**:
  - UC20: Dashboard cá nhân (thống kê số lượng ticket theo trạng thái của tôi).
  - UC02: Xem danh sách yêu cầu cá nhân (kèm bộ lọc trạng thái).
  - UC03: Tạo yêu cầu mới (khởi tạo ở trạng thái `New`, tự động kích hoạt Observer gửi thông báo tới Leader).
  - UC08: Xem chi tiết yêu cầu cá nhân.
  - UC06: Gửi bình luận trao đổi thông tin (bị chặn khi ticket ở trạng thái `Resolved` - Kịch bản 8).
  - UC04 & UC19: Đóng ticket và gửi đánh giá chất lượng (1-5 sao, feedback text - Kịch bản 9).

#### C. Area `Staff`
- **Controllers**: `DashboardController`, `WorkQueueController`
- **Use Cases**:
  - UC20: Dashboard kỹ thuật viên (hàng đợi công việc, ticket được giao).
  - UC08: Xem chi tiết yêu cầu được phân công.
  - **Bảo mật BOLA**: Nhân viên kỹ thuật chỉ được xem và cập nhật ticket được phân công cho chính mình (`AssigneeId == CurrentUserId`). Nếu truy cập ticket của người khác, trả về **403 Forbidden** (Kịch bản 7).
  - UC07: Cập nhật trạng thái xử lý (`Assigned` -> `InProgress` -> `Waiting` / `Resolved`). Bị từ chối nếu chuyển sai quy tắc (ví dụ: Staff không thể chuyển thẳng New -> Closed - Kịch bản 5).
  - UC06: Bình luận trao đổi hỗ trợ.
  - UC16: Xem lịch sử các bước xử lý.

#### D. Area `Leader`
- **Controllers**: `DashboardController`, `TicketManagementController`
- **Use Cases**:
  - UC11: Xem tổng quan hoạt động kỹ thuật (Dashboard thống kê toàn bộ ticket, tỷ lệ hoàn thành, SLA vi phạm).
  - UC12: Tra cứu toàn bộ ticket trong hệ thống.
  - UC09: Phân công ticket `New` cho Staff (chuyển sang `Assigned`, gửi thông báo cho cả Staff và User - Kịch bản 4). Hỗ trợ tự phân công (Leader tự xử lý) hoặc phân công lại.
  - UC10: Từ chối ticket (`Reject Ticket`). **Chỉ được phép từ chối khi ticket đang ở trạng thái `New`**. Tuyệt đối không được từ chối khi ticket đã `InProgress` (Kịch bản 6).
  - UC06: Tham gia bình luận trao đổi.

#### E. Area `Admin`
- **Controllers**: `DashboardController`, `UserController`, `SystemLogController`
- **Use Cases**:
  - UC11: Dashboard tổng hợp hệ thống (số lượng người dùng, hoạt động gần đây).
  - UC13 & UC14: Quản lý người dùng, tạo tài khoản mới (phân quyền User, Staff, Leader, Admin).
  - UC15: Kích hoạt / Vô hiệu hóa tài khoản người dùng (`Toggle User Status`). **Quy tắc bất biến: Admin có thể vô hiệu hóa tài khoản khác nhưng KHÔNG ĐƯỢC PHÉP tự vô hiệu hóa chính mình** (Kịch bản 10).
  - UC18: Đặt lại mật khẩu người dùng (Admin Reset Password).
  - UC16: Xem và tra cứu Nhật ký hệ thống (`SystemLogs`).

### 4.3. Thiết kế Giao diện Bootstrap 5 (Modern Enterprise UI)
- Sử dụng Bootstrap 5.3 (CSS và JavaScript bundle).
- Bố cục responsive chuyên nghiệp:
  - Sidebar bên trái hiển thị danh mục menu động theo Role hiện tại.
  - Topbar chứa nút ẩn hiện sidebar, avatar người dùng và biểu tượng Chuông Thông Báo với huy hiệu số lượng chưa đọc (cập nhật real-time qua SignalR).
  - Màu sắc huy hiệu trạng thái (Badges) chuẩn hóa:
    - `New`: `<span class="badge bg-primary">Mới tạo</span>`
    - `Assigned`: `<span class="badge bg-info text-dark">Đã phân công</span>`
    - `InProgress`: `<span class="badge bg-warning text-dark">Đang xử lý</span>`
    - `Waiting`: `<span class="badge bg-secondary">Chờ phản hồi</span>`
    - `Resolved`: `<span class="badge bg-success">Đã giải quyết</span>`
    - `Closed`: `<span class="badge bg-dark">Đã đóng</span>`
    - `Rejected`: `<span class="badge bg-danger">Bị từ chối</span>`

---

## 5. KHẢO SÁT CÀI ĐẶT 4 MẪU THIẾT KẾ PHẦN MỀM (DESIGN PATTERNS)

### 5.1. Mẫu Trạng thái (State Pattern) - Quản lý Vòng đời Ticket
- **Bối cảnh**: Vòng đời của ticket có các quy tắc nghiệp vụ nghiêm ngặt phụ thuộc vào vai trò người thực hiện và trạng thái hiện tại.
- **Cấu trúc Lớp**:
  - `Ticket` đóng vai trò Context.
  - `TicketStateBase` là lớp trừu tượng định nghĩa các hành vi chuyển đổi:
    ```csharp
    public abstract class TicketStateBase
    {
        public abstract TicketStatusEnum Status { get; }
        public virtual Result Assign(Ticket ticket, User leader, User staff) => Result.Failure(TicketErrors.InvalidTransition);
        public virtual Result StartWork(Ticket ticket, User staff) => Result.Failure(TicketErrors.InvalidTransition);
        public virtual Result SetWaiting(Ticket ticket, User staff, string reason) => Result.Failure(TicketErrors.InvalidTransition);
        public virtual Result Resolve(Ticket ticket, User staff, string solution) => Result.Failure(TicketErrors.InvalidTransition);
        public virtual Result CloseAndRate(Ticket ticket, User user, int rating, string? comment) => Result.Failure(TicketErrors.InvalidTransition);
        public virtual Result Reject(Ticket ticket, User leader, string reason) => Result.Failure(TicketErrors.InvalidTransition);
        public virtual Result Reopen(Ticket ticket, User user, string reason) => Result.Failure(TicketErrors.InvalidTransition);
        public virtual Result CanAddComment(Ticket ticket, User user) => Result.Success();
    }
    ```
  - 7 lớp cụ thể: `NewState`, `AssignedState`, `InProgressState`, `WaitingState`, `ResolvedState`, `ClosedState`, `RejectedState`.
- **Triết lý Railway-Oriented `Result<T>`**:
  - Không ném `InvalidOperationException` cho các thao tác sai trạng thái thông thường.
  - Trả về đối tượng `Result.Failure(new Error("INVALID_STATE_TRANSITION", "..."))` giúp controller dễ dàng hiển thị thông báo thân thiện mà không gây tốn tài nguyên unwinding stack trace (`#026 Bottleneck`).
- **Quy tắc chuyển đổi trọng yếu**:
  - Tại `NewState`: Cho phép `Assign` (chuyển `AssignedState`), cho phép `Reject` (chuyển `RejectedState`). Từ chối mọi hành vi khác (Staff không thể đổi sang Closed hoặc InProgress).
  - Tại `AssignedState`: Staff được giao có thể gọi `StartWork` (chuyển `InProgressState`). Leader có thể phân công lại.
  - Tại `InProgressState`: Staff có thể gọi `SetWaiting` (chuyển `WaitingState`) hoặc `Resolve` (chuyển `ResolvedState`). **Leader KHÔNG ĐƯỢC phép Reject** (chặn Kịch bản 6).
  - Tại `ResolvedState`: User xác nhận `CloseAndRate` (chuyển `ClosedState` kèm feedback). **Chặn không cho phép thêm Comment** (hàm `CanAddComment` trả về Failure - chặn Kịch bản 8).
  - Tại `ClosedState` và `RejectedState`: Các trạng thái kết thúc, không cho phép cập nhật tiến độ.

### 5.2. Mẫu Quan sát (Observer Pattern) - Thông báo và Nhật ký Hệ thống
- **Bối cảnh**: Khi trạng thái ticket thay đổi hoặc có bình luận mới, nhiều tác vụ phụ (gửi thông báo, đẩy SignalR, ghi log kiểm toán) cần được kích hoạt mà không làm gắn kết chặt chẽ (tight coupling) logic nghiệp vụ cốt lõi.
- **Cấu trúc Hiện thực**:
  - Domain Events phát ra từ Aggregate Root `Ticket`:
    - `TicketCreatedEvent(int ticketId, int requesterId, string title)`
    - `TicketAssignedEvent(int ticketId, int requesterId, int assigneeId, int leaderId)`
    - `TicketStatusChangedEvent(int ticketId, TicketStatusEnum oldStatus, TicketStatusEnum newStatus, int changedById)`
    - `TicketClosedAndRatedEvent(int ticketId, int rating, string? comment, int closedById)`
    - `CommentAddedEvent(int ticketId, int commentId, int authorId, string content)`
  - 2 Observers chính đăng ký lắng nghe sự kiện:
    1. **`NotificationObserver`**:
       - Tạo bản ghi trong bảng `Notifications`.
       - Phát sóng sự kiện tức thời qua SignalR tới user mục tiêu (`user_{id}`) hoặc nhóm kỹ thuật (`role_leader`).
    2. **`SystemLogObserver`**:
       - Ghi vết kiểm toán bất biến vào bảng `SystemLogs` (lưu lại UserId, Action, EntityName, EntityId, Details, IpAddress, Timestamp).

### 5.3. Mẫu Trang trí / Bộ lọc Hành động (Decorator / ActionFilter Pattern)
- **Cài đặt**: Lớp `AuthorizeRolesAttribute` kế thừa `TypeFilterAttribute` (hoặc `IAsyncActionFilter`).
- **Nhiệm vụ kiểm soát 2 tầng (Defense-in-Depth `#053`)**:
  1. **Kiểm tra trạng thái tài khoản (`IsActive`)**:
     - Lấy ID người dùng từ Claims, truy vấn trạng thái `IsActive` hiện tại từ CSDL (hoặc kiểm tra claims đã được xác thực).
     - Nếu tài khoản đã bị Admin vô hiệu hóa (`IsActive == false`), lập tức ép đăng xuất (`SignOutAsync`), chuyển hướng về trang Login kèm thông báo lỗi `"Tài khoản đã bị vô hiệu hóa"` (Đáp ứng Kịch bản 1).
  2. **Kiểm tra phân quyền vai trò (Role-Based Access Control)**:
     - So khớp vai trò của người dùng với danh sách các roles được phép (`[AuthorizeRoles(UserRole.Leader, UserRole.Admin)]`).
     - Nếu không có quyền, trả về trực tiếp mã lỗi **HTTP 403 Forbidden** hoặc hiển thị View 403 chuyên biệt (Đáp ứng Kịch bản 2).

### 5.4. Mẫu Đối tượng Duy nhất (Singleton Pattern)
- Tận dụng cơ chế DI nguyên bản của ASP.NET Core:
  - Đăng ký `TimeProvider.System` dạng Singleton (hỗ trợ kiểm thử thời gian giả lập qua `FakeTimeProvider`).
  - Đăng ký các bộ quản lý kết nối SignalR mapping hoặc cache dữ liệu tĩnh dạng Singleton.

---

## 6. KHẢO SÁT SIGNALR WEBSOCKET HUB & KẾT NỐI REAL-TIME

### 6.1. Khảo sát di sản (Vấn đề của Polling 3s trong Flask cũ)
- Trong tài liệu đồ án cũ (`extracted_doc_text.txt`, dòng 1581):
  > *"Gần thời gian thực & bất đồng bộ: khung trao đổi cập nhật bằng polling (/ticket/<id>/comments/poll)"*
- **Phân tích kỹ thuật theo `#Argon-Omega` (`#026 Bottleneck`)**:
  - Cơ chế Polling mỗi 3 giây gửi liên tục các request HTTP GET đến máy chủ ngay cả khi không có bất kỳ bình luận nào mới.
  - Với $N$ người dùng đang mở trang ticket, số request đến CSDL là $O(N)$ request mỗi 3 giây. Điều này gây lãng phí băng thông, bão hòa I/O kết nối CSDL và tạo ra độ trễ (delay từ 0 đến 3 giây để nhìn thấy phản hồi).
  - Theo chỉ thị bất biến trong `PROJECT_MEMORY.md`: **Tiêu diệt triệt để Anti-pattern Polling 3s**, thay thế 100% bằng SignalR WebSocket.

### 6.2. Thiết kế SignalR WebSocket Hub (`TicketHub`)
- **Lớp Hub**: `ItTms.Web.Hubs.TicketHub : Hub`
- **Tuyến đường (Route)**: `/hubs/ticket`
- **Quản lý Nhóm (Channel Isolation)**:
  - Khi người dùng mở xem một ticket chi tiết: Gọi `JoinTicketGroup(ticketId)` -> đưa kết nối vào Group `"ticket_" + ticketId`.
  - Khi rời trang: Gọi `LeaveTicketGroup(ticketId)`.
  - Mỗi người dùng đăng nhập tự động tham gia nhóm cá nhân `"user_" + userId` (để nhận thông báo riêng).
  - Lãnh đạo tự động tham gia nhóm `"role_leader"`.
- **Các phương thức phát sóng (Server to Client Events)**:
  - `ReceiveComment(CommentDto comment)`: Gửi đến Group `ticket_{id}` ngay khi có người gửi bình luận mới.
  - `ReceiveNotification(NotificationDto notification)`: Đẩy trực tiếp thông báo đến `user_{userId}` hoặc `role_leader`.
  - `TicketStatusUpdated(int ticketId, string statusCode, string statusName)`: Thông báo cho toàn bộ người đang xem ticket để cập nhật badge trạng thái mà không cần tải lại trang.

### 6.3. Script Phía Client (`ticketHub.js`)
- Tích hợp thư viện `@microsoft/signalr` (tải về cục bộ trong `wwwroot/lib/signalr/signalr.min.js`).
- Khởi tạo kết nối với cấu hình tự động kết nối lại:
  ```javascript
  const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/ticket")
      .withAutomaticReconnect()
      .build();
  ```
- Lắng nghe sự kiện `ReceiveComment` -> Tự động tạo phần tử DOM tin nhắn mới và chèn vào khung chat mượt mà, tự động cuộn xuống cuối.
- Lắng nghe `ReceiveNotification` -> Tăng biến đếm trên chuông thông báo ở thanh Navbar và hiển thị thông báo pop-up (Toast).

---

## 7. KHẢO SÁT TRẠNG THÁI BUILD & TEST (BUILD & TEST BASELINE)

### 7.1. Trạng thái Build hiện tại
- Chạy lệnh `dotnet build` trên thư mục `c:\IT_System`:
  - Chưa có solution/csproj file để thực thi.
- Môi trường SDK:
  - .NET SDK `9.0.317` hoạt động hoàn hảo, không có lỗi cấu hình.

### 7.2. Đặc tả 10 Kịch bản Kiểm thử Nghiệm thu (Bảng 7.2 của Báo cáo)
Bộ kiểm thử tự động `ItTms.UnitTests` cần được xây dựng để kiểm chứng và ghi nhận kết quả thực tế cho 10 kịch bản:

| STT | Kịch bản kiểm thử | Hành động & Dữ liệu kiểm thử | Kết quả mong đợi | Phương pháp nghiệm thu (.NET 9) |
| :---: | :--- | :--- | :--- | :--- |
| **1** | Đăng nhập bằng tài khoản đã bị vô hiệu hóa | Đăng nhập tài khoản `inactive_user` | Từ chối đăng nhập, hiển thị thông báo "Tài khoản đã bị vô hiệu hóa" | Unit/Integration Test: Gọi `AccountController.Login` với tài khoản `IsActive = false`, assert ModelState error & redirect |
| **2** | User truy cập trang chỉ dành cho Leader | User thường gửi request tới `/Leader/TicketManagement` | Trả về lỗi HTTP 403 Forbidden | Test `AuthorizeRolesAttribute`: Xác thực User không thuộc role Leader, assert `context.Result is ForbidResult` |
| **3** | User tạo ticket mới | User submit form tạo ticket | Ticket ở trạng thái `New`; Leader nhận được thông báo | Test `CreateTicket`: Ticket status = New (Id=1), Observer tạo bản ghi Notification cho Leader |
| **4** | Leader phân công ticket New cho Staff | Leader chọn Staff và bấm Phân công | Ticket chuyển sang `Assigned`; Staff và User nhận thông báo | Test `AssignTicket`: State machine chuyển New -> Assigned, Observer tạo 2 Notifications |
| **5** | Staff đặt trạng thái Closed cho ticket đang New | Staff gửi request đổi trạng thái sang Closed | Bị từ chối (Failure), dữ liệu giữ nguyên | State Machine Test: `NewState.Resolve()` hoặc `.CloseAndRate()` trả về `Result.Failure` |
| **6** | Leader từ chối ticket đang In Progress | Leader gọi action Reject trên ticket InProgress | Bị từ chối (Failure), dữ liệu giữ nguyên | State Machine Test: `InProgressState.Reject()` trả về `Result.Failure(TicketErrors.CannotRejectInProgress)` |
| **7** | Staff mở ticket không được phân công cho mình | Staff1 mở ticket được gán cho Staff2 | Trả về lỗi HTTP 403 Forbidden (BOLA) | Resource Authorization Test: So khớp `ticket.AssigneeId != currentStaff.Id`, trả về 403 |
| **8** | User bình luận trên ticket đã Resolved | User gửi bình luận vào ticket đã giải quyết | Bị chặn, không lưu bình luận vào CSDL | State Machine Test: `ResolvedState.CanAddComment()` trả về Failure, controller từ chối thêm comment |
| **9** | User xác nhận đóng và đánh giá ticket Resolved | User gửi rating (5 sao) + feedback | Ticket chuyển `Closed`; lưu Feedback; ghi SystemLog | Test `CloseAndRate`: Ticket chuyển Closed, tạo bản ghi Feedback, SystemLogObserver ghi log |
| **10**| Admin vô hiệu hóa tài khoản khác; thử tự vô hiệu hóa chính mình | Admin tắt user khác (thành công); Admin tắt chính mình (bị chặn) | Tài khoản kia bị khóa; tự vô hiệu hóa bị từ chối báo lỗi | Test `ToggleUserStatus`: `targetUserId == currentUserId` trả về Failure("Không thể tự vô hiệu hóa") |

---

## 8. PHÂN TÍCH KHOẢNG TRỐNG KIẾN TRÚC VÀ LỘ TRÌNH TRIỂN KHAI (GAP ANALYSIS & ROADMAP)

### 8.1. Ma trận So sánh Khoảng trống (Gap Matrix: As-Is vs To-Be)

| Thành phần | Hiện trạng trong kho thư mục (As-Is) | Mục tiêu kiến trúc yêu cầu (To-Be) | Mức độ khoảng trống | Hành động kỹ thuật cần thực thi |
| :--- | :--- | :--- | :---: | :--- |
| **Project & Solution** | 0 file .sln, 0 file .csproj | Solution .NET 9 chuẩn Clean Architecture gồm 5 projects | **100% Greenfield** | Dùng `dotnet new` scaffold `ItTms.sln` và 5 projects theo cấu trúc chuẩn |
| **Domain & Entities** | 0 class | 8 thực thể, Enums, Domain Events, State Pattern, Result<T> | **100% Greenfield** | Cài đặt tầng `ItTms.Domain` không phụ thuộc thư viện ngoài |
| **Database & EF Core** | 0 DbContext, 0 migrations, 0 db | EF Core 9 SQLite, Concurrency Token (`RowVersion`), Covering Indexes, Auto Seed | **100% Greenfield** | Cài đặt `ItTms.Infrastructure`, cấu hình Fluent API, tạo Migration và DatabaseSeeder |
| **Design Patterns** | Chưa cài đặt | 4 mẫu: State (7 states), Observer, Decorator/Filter, Singleton | **100% Greenfield** | Hiện thực hóa tại Domain, Application và Web Filters |
| **Web Presentation** | 0 Controller, 0 View | 5 Areas (Auth, User, Staff, Leader, Admin), 20 Use Cases, Bootstrap 5 UI | **100% Greenfield** | Cài đặt Controllers, ViewModels, Razor Views và Layout responsive |
| **Real-time Comms** | Tài liệu cũ dùng Polling 3s | SignalR WebSocket `TicketHub` + `ticketHub.js` | **100% Greenfield** | Cài đặt Hub endpoint `/hubs/ticket` và client script tự động cập nhật |
| **Unit & E2E Testing** | 0 test project | Bộ xUnit test tự động bao phủ State Machine và 10 kịch bản Bảng 7.2 | **100% Greenfield** | Cài đặt `ItTms.UnitTests`, viết đầy đủ test cases kiểm chứng |
| **Tài liệu Báo cáo Word**| File docx chứa nội dung Flask cũ | Đồng bộ toàn bộ nội dung sang .NET 9 / C# / SignalR, bảo toàn 100% TOC | **Cần đồng bộ (Phase B)** | Biên tập file docx thay thế code và kết quả thực nghiệm sau khi Phase A hoàn tất |

### 8.2. Lộ trình Triển khai Đề xuất cho Orchestrator
Theo mô hình **Dual Track (Implementation Track + E2E Testing Track)**:

1. **Milestone 1: Project Scaffolding & Domain Core Foundation**:
   - Khởi tạo `ItTms.sln` và 5 dự án thành phần với target framework `net9.0`.
   - Cài đặt `ItTms.Domain`: Base classes, `Result<T>`, 8 Thực thể, Enums, Domain Events.
   - Cài đặt State Pattern: `TicketStateBase` cùng 7 concrete states.
2. **Milestone 2: Infrastructure, EF Core 9 & Database Seeding**:
   - Cài đặt `ApplicationDbContext`, Fluent API mappings, Concurrency Token (`RowVersion`), Covering Indexes.
   - Tạo initial migration SQLite (`it_tms.db`).
   - Viết `DatabaseSeeder` tự động nạp 7 trạng thái và các tài khoản mẫu cho 4 vai trò.
3. **Milestone 3: Real-Time SignalR Hub & Application Services**:
   - Cài đặt `TicketHub` và các phương thức tham gia Group theo TicketId và UserId.
   - Cài đặt Observer Pattern: `NotificationObserver` và `SystemLogObserver`.
4. **Milestone 4: Presentation Layer - 5 MVC Areas & Bootstrap 5 UI**:
   - Hiện thực hóa `AuthorizeRolesAttribute` kiểm soát RBAC và cờ `IsActive`.
   - Cài đặt Controllers và Views trong 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) bao phủ 20 Use Cases.
   - Tích hợp Bootstrap 5 và client-side script `ticketHub.js`.
5. **Milestone 5: Verification & Hardening (Phase A Completion)**:
   - Viết và chạy 100% các bài test trong `ItTms.UnitTests` chứng minh vượt qua 10 kịch bản kiểm thử trong Bảng 7.2.
   - Chạy `dotnet build` đạt 0 cảnh báo, 0 lỗi.
6. **Milestone 6: Document Synchronization (Phase B)**:
   - Cập nhật file báo cáo `BaoCao_ITTMS_TKPMHDT (4).docx`.
   - Thay thế kiến trúc Python/Flask bằng C# / .NET 9 / SignalR.
   - Cập nhật Bảng 7.1 (Công nghệ), Bảng 7.2 (Điền kết quả thực tế Đạt), Bảng 6.4 (Vị trí áp dụng Pattern).
   - Bảo toàn tuyệt đối 100% cấu trúc thứ tự và tiêu đề các chương mục.

---

## 9. KẾT LUẬN & KIẾN NGHỊ KỸ THUẬT

1. **Khẳng định về Scope**: Dự án là công việc phát triển mới (Greenfield Implementation) kết hợp kế thừa phân tích đặc tả nghiệp vụ từ tài liệu đồ án HCMUTE sẵn có. Không có mã nguồn cũ gây vướng mắc kỹ thuật, cho phép áp dụng ngay các chuẩn mực thiết kế tốt nhất của .NET 9.
2. **Tuân thủ Invariant**:
   - Tuyệt đối không tái sử dụng Polling 3s dưới bất kỳ hình thức nào.
   - Đảm bảo tính nhất quán dữ liệu bằng Concurrency Token và State Pattern đóng gói chặt chẽ.
   - Kiểm soát quyền truy cập chặt chẽ (chống BOLA tại tầng Controller và Filter).
3. Báo cáo này đã sẵn sàng cung cấp cơ sở dữ liệu kỹ thuật đầy đủ để Orchestrator lập kế hoạch chi tiết trong `PROJECT.md` và điều phối các worker tiếp theo.
