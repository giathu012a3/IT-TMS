# Original User Request

## 2026-10-25T07:53:40Z

Build a production-grade IT Ticket Management System (IT-TMS) in .NET 9 using Clean Architecture and Design Patterns, featuring real-time SignalR communication, a redesigned 8-table SQLite database schema, Bootstrap 5 UI, and synchronize the project report document while strictly preserving its original section hierarchy.

Working directory: c:\IT_System
Integrity mode: development

---

## Requirements

### R1. Full-Stack .NET 9 Web Application & 20 Use Cases
Implement a complete ASP.NET Core MVC solution with 5 Areas (Auth, User, Staff, Leader, Admin) covering all 20 Use Cases (UC01 to UC20), including authentication, personal/team ticket queues, assignment, status tracking, rating feedback, user management, and role-based analytics dashboards. Design the views using Bootstrap 5 for a clean, accessible enterprise UI.

### R2. Re-Engineered SQLite Database Schema & Concurrency (EF Core 9)
Implement the 8 core entities (Users, TicketStatuses, Tickets, Comments, Feedbacks, Attachments, Notifications, SystemLogs) with SQLite via EF Core 9 for zero-setup, self-contained execution. Incorporate optimistic concurrency tokens, SLA tracking timestamps (DueAt, ResolvedAt), attachment file metadata, and covering indexes. Automatically seed default roles, users, and ticket statuses on startup.

### R3. Software Design Patterns Implementation
- State Pattern: Enforce ticket lifecycle transitions across 7 states (New, Assigned, InProgress, Waiting, Resolved, Closed, Rejected) with strict encapsulation of allowed actions and railway-oriented Result<T> error reporting.
- Observer Pattern: Trigger NotificationObserver and SystemLogObserver on domain state mutations.
- Decorator / ActionFilter Pattern: Custom AuthorizeRolesAttribute enforcing RBAC and blocking inactive accounts.
- Singleton Pattern: Core shared services via ASP.NET Core Dependency Injection.

### R4. Real-Time SignalR WebSocket Hub (TicketHub)
Implement real-time bidirectional communication using ASP.NET Core SignalR for ticket discussion comments and instant notification badges, completely eliminating polling mechanisms.

### R5. Sequential Two-Phase Execution & Word Report Synchronization
- Phase A: Fully build, test, and verify the .NET 9 codebase until all features run cleanly and pass the 10 core verification scenarios from Table 7.2.
- Phase B: Update BaoCao_ITTMS_TKPMHDT (4).docx to replace legacy Python/Flask content with the .NET 9 / C# / SignalR architecture, updated database tables, C# code snippets, and actual test results, strictly preserving 100% of the original document outline and section numbering.

---

## Acceptance Criteria

### Build & Architecture
- [ ] Solution compiles with zero warnings/errors via dotnet build.
- [ ] Database automatically creates the SQLite database file and seeds test accounts for all 4 roles on application startup.

### Functional Verification & Testing
- [ ] All 10 verification scenarios from Table 7.2 of the report pass verification:
  1. Inactive account login is rejected with an appropriate message.
  2. Accessing unauthorized role paths returns HTTP 403 Forbidden.
  3. Creating a ticket initializes it in New state and notifies the Leader.
  4. Assigning a ticket moves state to Assigned and notifies both Staff and User.
  5. Invalid state transitions (e.g. Staff setting Closed on a New ticket) are rejected.
  6. Leader cannot reject an InProgress ticket.
  7. Staff cannot access tickets not assigned to them.
  8. Comments on a Resolved ticket are blocked.
  9. Closing and rating a Resolved ticket transitions it to Closed with feedback and log recorded.
  10. Admin can toggle other accounts but cannot deactivate their own account.
- [ ] Unit tests for the State Machine pass via dotnet test.

### Real-Time & Security
- [ ] SignalR TicketHub establishes WebSocket connection and broadcasts comments to the appropriate ticket group without page reloads.
- [ ] Decorator attribute blocks unauthorized roles and inactive accounts across all protected endpoints.

### Report Integrity
- [ ] The updated Word report contains all original chapter and section headings without structural changes.
- [ ] Table 7.1 (Tech Stack: .NET 9, SQLite, EF Core 9, SignalR, Bootstrap 5), Table 7.2 (Test Scenarios with verified results), and Chapter 6 (Design Patterns) accurately reflect the .NET 9 implementation.


---

## 2026-10-25T13:17:29Z

Hoàn thiện toàn diện hệ thống giao diện và bảng màu Enterprise Neutral Light & Carbon Dark trên toàn bộ 23 Razor Views thuộc 5 Areas (`Auth`, `User`, `Staff`, `Leader`, `Admin`) của hệ thống quản lý yêu cầu hỗ trợ kỹ thuật IT-TMS (.NET 9 MVC), thiết kế lại toàn bộ hệ thống thông báo Thành công / Thất bại / Cảnh báo / SignalR sang dạng **Floating Card Box (Hộp thẻ nổi nhiều tầng góc trên bên phải)**, và **khắc phục triệt để lỗi khung Chat Real-time SignalR** (nhấn Enter gửi ngay lập tức, hiển thị tức thì bên người gửi và đẩy thời gian thực sang bên người nhận).

Working directory: c:\IT_System
Integrity mode: development

## Verification Resources
- Giải pháp hiện có: `c:\IT_System\ItTms.sln` (5 projects: `ItTms.Domain`, `ItTms.Application`, `ItTms.Infrastructure`, `ItTms.Web`, `ItTms.Tests`).
- Bộ kiểm thử tự động: `dotnet test c:\IT_System\ItTms.sln` (336 kịch bản kiểm thử xUnit, bao gồm 10 kịch bản nghiệm thu Bảng 7.2).
- Tài liệu bộ nhớ kiến trúc & bất biến dự án: `c:\IT_System\PROJECT_MEMORY.md` và `c:\IT_System\PROJECT.md`.

## Requirements

### R1. Khắc phục triệt để lỗi Khung Chat Real-time SignalR & Phím tắt Enter (`User`, `Staff`, `Leader`)
Hiện tại khung thảo luận Ticket trên cả 3 phân hệ (`User/Ticket/Details`, `Staff/StaffTicket/Details`, `Leader/LeaderTicket/Details`) đang mắc 3 lỗi nghiêm trọng khiến việc nhắn tin không hoạt động thời gian thực:
1. **Nhấn phím Enter không gửi tin nhắn**: Ô nhập `#comment-content-input` là thẻ `<textarea>` có placeholder ghi `(Enter gửi)`, nhưng trong `wwwroot/js/ticket-realtime.js` chưa bắt sự kiện `keydown` phím `Enter` (không giữ `Shift`) để kích hoạt gửi form, khiến nhấn Enter chỉ xuống dòng.
2. **Backend `AddComment` không phát sự kiện SignalR sang người nhận**: Trong `LeaderTicketController.AddComment`, `StaffTicketController.AddComment` và `User/TicketController.AddComment`, khi tạo bình luận qua `Comment.Create(...)` và lưu DB, không có `CommentAddedEvent` nào được gắn vào `ticket.AddDomainEvent(...)` (hoặc bắn qua `INotificationDispatcher` / `IHubContext<TicketHub>`), dẫn tới `SignalRNotificationDispatcher.HandleCommentAddedAsync`, `NotificationObserver` và `SystemLogObserver` không bao giờ được kích hoạt khi gửi qua HTTP POST! Hệ quả là cả người gửi lẫn người nhận đều không nhận được sự kiện `ReceiveComment` hay thông báo chuông `ReceiveNotification`.
3. **Cơ chế hiển thị tức thì & chống trùng lặp (Deduplication)**: Khi người gửi nhấn Enter hoặc bấm nút Gửi, tin nhắn phải hiển thị ngay lập tức trên khung chat của người gửi (kết hợp trả về `CommentDto` từ response JSON của `AddComment` và sự kiện `ReceiveComment` qua SignalR WebSocket, có kiểm tra chống trùng lặp theo `data-comment-id`), đồng thời bên màn hình người nhận đang mở cùng Ticket (hoặc chuông thông báo toàn cục) phải nhận được tin nhắn tức thì mà không cần tải lại trang.

### R2. Rà soát & Đồng bộ triệt để Bảng màu Enterprise Neutral Light & Carbon Dark trên toàn bộ 23 Views (5 Areas)
Kiểm tra và hoàn thiện toàn bộ các màn hình hiện còn sót chưa được đồng bộ bảng màu trong `src/ItTms.Web` (bao gồm `AdminDashboard/Index`, `Admin/User/Index`, `Admin/User/Create`, `Admin/User/Edit`, `Admin/Role/Index`, `Admin/Audit/Index`, `LeaderDashboard/Index`, `LeaderTicket/NewQueue`, `LeaderTicket/Index`, `LeaderTicket/Details`, `Staff/Dashboard/Index`, `StaffTicket/Assigned`, `StaffTicket/Details`, `StaffTicket/History`, `User/Dashboard/Index`, `User/Ticket/Index`, `User/Ticket/Create`, `User/Ticket/Details`, `Auth/Login`, `Auth/ChangePassword`, `Shared/_Layout`, `Shared/_StatusBadge`, `Shared/_SlaBadge`, `Shared/AccessDenied`, `Shared/Error`).
- Rút toàn bộ các màu Bootstrap cứng gây nhiễu thị giác khỏi khung vỏ và văn bản tĩnh: xóa `text-primary` và `text-danger` trên các tiêu đề trang/thẻ (`h1`–`h6`), chuyển cột mã `#ID` từ `text-primary` sang phông chữ monospace trung tính sắc nét, chuyển các biểu tượng KPI trên mọi Dashboard về tông trung tính đồng bộ, và đồng bộ cột trái trang `Login` hài hòa với hệ màu Carbon.
- Áp dụng nghiêm ngặt quy tắc tín hiệu dữ liệu: mức độ ưu tiên `Low` và `Medium` trên mọi bảng và trang chi tiết chỉ dùng pill trung tính (`bg-secondary-subtle`), dành màu cảnh báo (`Critical`, `High`, `Quá hạn SLA`, trạng thái ticket) cho đúng dữ liệu nghiệp vụ với độ tương phản chữ/nền đạt chuẩn WCAG AA (`>= 4.5:1`) ở cả Light Mode lẫn Dark Mode.

### R3. Thiết kế lại Hệ thống Thông báo Thành công / Thất bại / Cảnh báo / SignalR dạng Floating Card Box Nhiều Tầng
Loại bỏ hoàn toàn kiểu hiển thị thông báo 1 dòng trải ngang (`alert py-2 px-3` trong `_ToastAlerts.cshtml`, thông báo lỗi 1 dòng trên `Login.cshtml`, các hộp thoại `window.alert()` thô sơ trong `ticket-realtime.js` / `signalr-notification.js`, và toast 1 dòng của SignalR).
- Thiết kế hệ thống **Floating Notification Card Box** neo cố định ở góc trên bên phải màn hình (`top-right`), hỗ trợ xếp chồng mượt mà (stacking) cho cả thông báo từ Server (`TempData["SuccessMessage"]`, `TempData["ErrorMessage"]`, `TempData["WarningMessage"]`, lỗi xác thực form/đăng nhập) lẫn thông báo đẩy thời gian thực từ SignalR và JavaScript.
- Mỗi Notification Box phải có cấu trúc thẻ nổi nhiều tầng (multi-layer card box): khung thẻ có viền sắc nét và đổ bóng nổi (`shadow-float`), dải màu chỉ thị ngữ nghĩa bên cạnh hoặc đầu hộp, khối Icon SVG trạng thái nổi bật trong khung tròn/bo góc, dòng **Tiêu đề phân loại** tách biệt rõ với **Nội dung chi tiết** bên dưới, nút hành động/đóng (`Close`) và **thanh tiến trình đếm ngược (auto-dismiss progress bar ~5s)** tự động tạm dừng khi di chuột vào (hover pause).

### R4. Bảo toàn 100% Nghiệp vụ & Bộ Kiểm thử Tự động
Mọi chỉnh sửa trên Domain, Application, Controllers, Razor Views, CSS và JavaScript phải giữ nguyên tính toàn vẹn của 20 Use Cases và vượt qua 100% bộ kiểm thử xUnit hiện có (cập nhật/bổ sung thêm bài kiểm thử cho luồng phát sự kiện `CommentAddedEvent` khi gửi bình luận).

## Acceptance Criteria

### 1. Khung Chat Real-time SignalR & Phím tắt Enter
- [ ] Trong `#comment-content-input`, nhấn `Enter` (không giữ `Shift`) tự động gửi bình luận ngay lập tức; nhấn `Shift + Enter` xuống dòng bình thường.
- [ ] Khi gửi bình luận qua `AddComment` ở cả 3 vai trò (`User`, `Staff`, `Leader`), sự kiện `CommentAddedEvent` được phát đi đầy đủ để: (1) đẩy `ReceiveComment` qua SignalR tới phòng `ticket_{id}` (hoặc `ticket_{id}_internal` nếu là ghi chú nội bộ), (2) đẩy thông báo `ReceiveNotification` tới người nhận liên quan, và (3) ghi nhận nhật ký hệ thống.
- [ ] Tin nhắn mới xuất hiện ngay lập tức trên màn hình người gửi sau khi nhấn Enter và xuất hiện thời gian thực trên màn hình người nhận đang mở cùng Ticket mà không cần reload trang, đồng thời không bị nhân đôi (duplicate) bong bóng chat trên màn hình người gửi.
- [ ] Ghi chú nội bộ (`IsInternal = true`) chỉ hiển thị cho `Staff`, `Leader`, `Admin` và tuyệt đối không phát sang màn hình của `User`.

### 2. Đồng bộ Bảng màu & Hoàn thiện toàn bộ 23 Views
- [ ] Quét toàn bộ thư mục `src/ItTms.Web` xác nhận không còn thẻ tiêu đề (`h1`–`h6`) hoặc cột mã `#@ticket.Id` / `#@t.Id` nào dùng class `text-primary` hoặc `text-danger` cứng.
- [ ] Quét toàn bộ các Views trong `Admin`, `Leader`, `Staff`, `User` xác nhận mức độ ưu tiên `Low` và `Medium` đều hiển thị bằng pill trung tính (`bg-secondary-subtle`), không còn nơi nào dùng `bg-primary` cho mức ưu tiên thấp/trung bình.
- [ ] Các thẻ KPI trên cả 4 Dashboard (`Admin`, `Leader`, `Staff`, `User`) có cấu trúc và màu sắc biểu tượng đồng nhất, không còn icon nhiều màu lộn xộn.
- [ ] Kiểm tra độ tương phản thực tế bằng script đo màu trên trình duyệt xác nhận 100% các `.badge`, `.btn` và chữ hiển thị trên các màn hình (`Login`, `Admin`, `Leader`, `Staff`, `User`) ở cả Light Mode (`data-bs-theme="light"`) và Dark Mode (`data-bs-theme="dark"`) đều đạt tỷ lệ tương phản `>= 4.5:1`.

### 3. Hệ thống Floating Card Box Thông báo
- [ ] `_ToastAlerts.cshtml` và JS helper thống nhất hiển thị mọi thông báo Thành công (`Success`), Thất bại (`Error`), Cảnh báo (`Warning`) và Thông tin (`Info` / `SignalR`) dưới dạng **Floating Card Box góc trên bên phải** gồm đủ 4 thành phần: Khối Icon SVG + Dòng Tiêu đề in đậm + Dòng Nội dung mô tả + Thanh tiến trình đếm ngược tự đóng (5s).
- [ ] Không còn bất kỳ lệnh `alert(...)` mặc định nào của trình duyệt trong `src/ItTms.Web/wwwroot/js/ticket-realtime.js` và `src/ItTms.Web/wwwroot/js/signalr-notification.js`; mọi thông báo lỗi gửi bình luận hoặc cưỡng chế đăng xuất đều hiển thị qua Floating Card Box.
- [ ] Không còn thẻ `<i class="bi ...">` bị lỗi thiếu font icon trong `signalr-notification.js` hay `ticket-realtime.js`; 100% icon trong Notification Box sử dụng SVG inline hoặc SVG sprite (`_SvgIcons.cshtml`).
- [ ] Trên màn hình `Login.cshtml`, khi đăng nhập sai mật khẩu hoặc tài khoản bị vô hiệu hóa (`TC-01`), thông báo lỗi xuất hiện rõ ràng dưới dạng Floating Card Box chuyên nghiệp mà không làm xô lệch bố cục 2 cột toàn màn hình (`.auth-split-shell`).

### 4. Nghiệm thu Build, Test & Trình duyệt Thực tế
- [ ] Chạy `dotnet build c:\IT_System\ItTms.sln` thành công với 0 lỗi (0 errors).
- [ ] Chạy `dotnet test c:\IT_System\ItTms.sln` vượt qua 100% (toàn bộ >= 336 tests PASS, bao gồm trọn vẹn 10 kịch bản `Table72VerificationTests`).
- [ ] Khởi chạy ứng dụng `ItTms.Web` và kiểm thử E2E thực tế trên trình duyệt xác nhận: (1) Gửi chat bằng phím Enter hiển thị tức thì ở cả người gửi và người nhận qua SignalR, (2) Floating Card Box thông báo hiển thị đúng chuẩn ở cả Light/Dark Mode, (3) 0 lỗi Console/Runtime.


---

## 2026-10-25T14:44:33Z

Conduct a comprehensive quality audit and end-to-end verification of the production-grade IT Ticket Management System (IT-TMS) in .NET 9 (C# 13) at c:\IT_System:

Working directory: c:\IT_System
Integrity mode: development

## Requirements

### R1. UI/UX & Frontend Verification across all 5 Areas
Verify that all 5 Areas (Areas/Auth, Areas/Admin, Areas/Leader, Areas/Staff, Areas/User) comply with Taste Skill v2 (achromatic-ledger / true-black-flat):
- Quick-fill buttons on Login page have readable contrast.
- No redundant footer in _Layout.cshtml.
- Compact flash alert container (max-width: 460px) with auto-dismiss.
- Admin user management table sorted by ID ascending with uniform action buttons.
- KPI cards structured cleanly via <section class="kpis">.

### R2. Report Integrity & WordprocessingML Synchronization
Verify that c:\IT_System\BaoCao_ITTMS_TKPMHDT (4).docx and c:\IT_System\BaoCao_ITTMS_TKPMHDT_DotNet9.docx:
- Can be opened without XML namespace corruption or schema errors in Microsoft Word.
- Preserve 100% of original 373 headings and Table of Contents hierarchy.
- Accurately document .NET 9, Clean Architecture, EF Core 9, SignalR, and 10/10 passing scenarios of Table 7.2.

### R3. Solution Build & Automated Test Suite Verification
Verify that c:\IT_System\ItTms.sln builds with 0 warnings, 0 errors, and all 342 automated tests pass (including 10/10 in Table72VerificationTests.cs).

## Acceptance Criteria
- [ ] Zero unreadable low-contrast text or broken styling in Dark Mode across all 5 Areas.
- [ ] Flash alert banners are constrained to max-width: 460px.
- [ ] Admin User Management action buttons align vertically without jagged shifting.
- [ ] Word .docx file contains valid XML namespaces with 0 synthetic ns0/ns1 prefixes and 373/373 headings intact.
- [ ] dotnet build passes with 0 Warning(s) and 0 Error(s).
- [ ] dotnet test passes 100% of 342 test cases.


---

## 2026-10-26T09:01:24Z

Thực hiện đồng bộ hóa toàn diện tài liệu báo cáo Word của hệ thống IT-TMS (.NET 9 ASP.NET Core MVC):
1. Cập nhật Bảng phân công công việc (Bảng PL.1) theo 4 phân hệ dọc độc lập (Vertical Slices: 1 Người = 1 Phân hệ = 1 Controller = 1 Mẫu thiết kế GoF), loại bỏ các nội dung ngoài lề môn học (unit test/devops), đảm bảo không chồng chéo mã nguồn và phục vụ vấn đáp tối ưu với Giảng viên.
2. Vẽ mới và chèn toàn bộ các sơ đồ kỹ thuật đang còn thiếu (ERD CSDL 8 bảng, BPMN quy trình xử lý ticket, State Machine 7 trạng thái, Class Diagram tổng thể, Sequence UC09, Class Diagram State Pattern).
3. Hiện đại hóa và đồng bộ hóa các sơ đồ cũ (Use Case diagrams Chương 2 và Sequence diagrams Chương 5) theo đúng mã nguồn C# .NET 9, loại bỏ triệt để các hình ảnh cũ/mờ hoặc tàn dư hệ thống cũ để toàn bộ báo cáo đạt tính đồng nhất cao nhất.

Working directory: `c:\IT_System`  
Integrity mode: development  

---

## Requirements

### R1. Chuẩn hóa Bảng phân công công việc (Bảng PL.1) theo 4 phân hệ độc lập (Zero Overlap)
Cập nhật nội dung Bảng PL.1 ở cả 2 tài liệu Word (`BaoCao_ITTMS_TKPMHDT (4).docx` và `BaoCao_ITTMS_TKPMHDT_DotNet9.docx`) theo đúng phân chia 4 phân hệ độc lập, mỗi thành viên làm chủ trọn vẹn 1 phân hệ từ Controller, Views, Nghiệp vụ đến Mẫu thiết kế GoF:
1. **Nguyễn Gia Thụ (25810041)**:
   - **Phân hệ**: **Kỹ thuật viên (Staff)**.
   - **Mã nguồn sở hữu**: `StaffController.cs`, `Views/Staff/` *(Dashboard, Assigned, Details, History)*.
   - **Mẫu thiết kế & Sơ đồ**: **State Pattern** (7 trạng thái vòng đời ticket), **Sơ đồ chuyển trạng thái (State Machine - Hình 5.2)**, **Lược đồ lớp State Pattern (Hình 6.1)**, **Lược đồ tuần tự UC07 (Hình 5.7)**.
2. **Nguyễn Mạnh Tiến (25810043)**:
   - **Phân hệ**: **Trưởng nhóm (Leader)**.
   - **Mã nguồn sở hữu**: `LeaderController.cs`, `Views/Leader/` *(Dashboard, NewQueue, Index, Details)*.
   - **Mẫu thiết kế & Sơ đồ**: **Factory Method / Flyweight Pattern** (`TicketStateFactory.cs`), **Quy trình xử lý BPMN (Hình 5.1)**, **Lược đồ tuần tự UC09 Phân công (Hình 5.6)**, **Lược đồ tuần tự UC10 Từ chối (Hình 5.9)**.
3. **Hồ Thanh Tình (25810044)**:
   - **Phân hệ**: **Người dùng (User)**.
   - **Mã nguồn sở hữu**: `TicketController.cs`, `Views/Ticket/` *(Dashboard, Index, Create, Details)*.
   - **Mẫu thiết kế & Sơ đồ**: **Observer Pattern** (Bắn thông báo tức thì qua SignalR WebSocket `TicketHub` & lưu nhật ký SystemLog), **Lược đồ tuần tự UC03 Tạo ticket (Hình 5.5)**, **Lược đồ tuần tự UC06 Chat trực tiếp (Hình 5.8)**, **Lược đồ tuần tự UC04/19 Đánh giá phản hồi (Hình 5.10)**.
4. **Trần Thanh Danh (25810009)**:
   - **Phân hệ**: **Quản trị & Bảo mật (Admin & Auth)**.
   - **Mã nguồn sở hữu**: `AdminController.cs`, `AuthController.cs`, `Views/Admin/`, `Views/Auth/`.
   - **Mẫu thiết kế & Sơ đồ**: **Decorator Pattern** (`[AuthorizeRoles]`), **Singleton Pattern** (`PasswordHasherService`), **Lược đồ thực thể – quan hệ ERD 8 bảng (Hình 3.1)**, **Lược đồ lớp tổng quát Class Diagram (Hình 5.3)**, **Lược đồ Use Case tổng thể (Hình 2.1 - 2.5)**.

---

### R2. Thiết kế mới và chèn toàn bộ các sơ đồ kỹ thuật đang thiếu
Sinh file hình ảnh độ nét cao chuẩn UML, nền sáng sang trọng, bám sát 100% C# .NET 9 và chèn đúng vị trí trước caption:
- **Hình 3.1**: Lược đồ thực thể – quan hệ (ERD) hoàn chỉnh gồm 8 bảng CSDL (`Users`, `Tickets`, `TicketStatuses`, `Comments`, `Feedbacks`, `Attachments`, `Notifications`, `SystemLogs`).
- **Hình 5.1**: Quy trình xử lý ticket từ lúc tạo đến khi kết thúc (BPMN / Activity Flow qua 7 trạng thái).
- **Hình 5.2**: Sơ đồ chuyển trạng thái của ticket (State Machine Diagram biểu diễn các transition nghiệp vụ).
- **Hình 5.3**: Lược đồ lớp tổng quát (Class Diagram tổng thể hệ thống ASP.NET Core MVC).
- **Hình 5.6**: Lược đồ tuần tự UC09 – Phân công yêu cầu (Sequence Diagram: Leader → LeaderController → TicketHub / DB).
- **Hình 6.1**: Cấu trúc lớp State Pattern (`ITicketState`, `TicketStateBase`, 7 concrete state classes).

---

### R3. Hiện đại hóa và đồng bộ hóa các sơ đồ cũ sang chuẩn .NET 9
Thay thế các hình vẽ cũ mờ/tàn dư hệ thống cũ (Hình 2.1 đến 2.5 Use Case diagrams, Hình 5.4, 5.5, 5.7, 5.8, 5.9, 5.10, 5.11, 5.12 Sequence diagrams) bằng các sơ đồ UML mới, sắc nét, thể hiện đúng các class, controller, method và kết nối WebSocket SignalR của hệ thống .NET 9 hiện tại.

---

## Acceptance Criteria

### Bảng phân công công việc
- [ ] Bảng PL.1 ở cả 2 tài liệu Word được cập nhật chính xác theo 4 phân hệ độc lập, không còn đề cập đến unit tests hay devops.
- [ ] Bảo toàn 100% (149/149) tiêu đề và cấu trúc mục lục (TOC) của tài liệu Word.

### Tính đồng nhất và chất lượng hình ảnh
- [ ] Toàn bộ 41 hình ảnh trong tài liệu (Hình 2.1-2.5, Hình 3.1, Hình 4.1-4.22, Hình 5.1-5.12, Hình 6.1) đều sắc nét, đồng bộ phong cách thiết kế, phản ánh chính xác 100% kiến trúc .NET 9 đang chạy.
- [ ] Không còn bất kỳ vị trí hình nào bị thiếu (MISSING).
- [ ] Cả 2 file `.docx` mở mượt mà trên Microsoft Word, không có lỗi cấu trúc XML.
