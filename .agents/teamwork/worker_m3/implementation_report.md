# BÁO CÁO TRIỂN KHAI CHI TIẾT — MILESTONE 3 (PRESENTATION LAYER & SIGNALR)

**Tác giả**: `worker_m3` (Implementer / QA / Specialist)  
**Thời gian hoàn thành**: 2026-10-25T10:28:00Z  
**Giải pháp**: `c:\IT_System\ItTms.sln`  
**Dự án triển khai chính**: `src/ItTms.Web/`  
**Dự án kiểm thử**: `tests/ItTms.Tests/`

---

## 1. TỔNG QUAN KIẾN TRÚC MILESTONE 3

Milestone 3 hoàn tất toàn bộ Tầng Trình diễn (Presentation Layer) của hệ thống IT Ticket Management System (IT-TMS) trên nền tảng .NET 9 theo chuẩn **Clean Architecture** và các mẫu thiết kế GoF (State, Observer, Decorator/Filter, Singleton):

1. **Cấu trúc 5 Phân vùng (5 Areas ASP.NET Core MVC)**:
   - `Areas/Auth/`: Quản lý xác thực phiên làm việc Cookie Authentication, đăng nhập, đổi mật khẩu, xử lý truy cập trái quyền (UC01, UC02, UC03).
   - `Areas/User/`: Phục vụ người dùng cuối tạo yêu cầu, theo dõi tiến độ, trao đổi real-time, đóng và đánh giá chất lượng dịch vụ (UC04, UC05, UC06, UC07, UC08, UC09).
   - `Areas/Staff/`: Cung cấp giao diện kỹ thuật viên IT tiếp nhận sự cố, cập nhật tiến độ, giải quyết ticket, ghi chú nội bộ kỹ thuật (UC06, UC07, UC10, UC11, UC12, UC13, UC16).
   - `Areas/Leader/`: Trung tâm điều phối hàng đợi yêu cầu mới, phân công nhân sự, điều chuyển kỹ thuật viên, từ chối yêu cầu, giám sát chỉ số SLA (UC14, UC15, UC16, UC17).
   - `Areas/Admin/`: Quản trị toàn diện người dùng, phân quyền RBAC, theo dõi nhật ký kiểm toán hệ thống (Audit Trail), thống kê tổng quan (UC18, UC19, UC20).

2. **Hệ thống WebSocket Thời gian thực (SignalR `TicketHub`)**:
   - Endpoint: `/hubs/ticket`.
   - **Triệt tiêu 100% cơ chế Polling định kỳ 3 giây**, chuyển đổi toàn diện sang Event-Driven WebSocket hai chiều.
   - Quản trị nhóm kết nối đa cấp: `user_{userId}`, `role_{roleName}`, `ticket_{ticketId}`, `ticket_{ticketId}_internal`.
   - Cơ chế phòng thủ **BOLA / IDOR Defense** ngay tại phương thức `JoinTicketGroup`: kiểm tra quyền sở hữu hoặc phân công trước khi cấp phép client lắng nghe dữ liệu thảo luận.
   - Cầu nối `SignalRNotificationDispatcher` hiện thực hóa `INotificationDispatcher` phát sóng đồng thời các sự kiện miền (`CommentAdded`, `TicketStatusChanged`, `TicketCreated`, `TicketAssigned`, `TicketClosed`).

3. **Giao diện Bootstrap 5 Hiện đại & Trải nghiệm Người dùng (UX)**:
   - Theme toggle sáng/tối (Dark/Light mode) tích hợp thuộc tính `data-bs-theme` của Bootstrap 5 và `localStorage`.
   - Hệ thống biểu tượng vector nhúng nội bộ (`_SvgIcons.cshtml`), không phụ thuộc tài nguyên bên ngoài.
   - Thư viện client `microsoft-signalr` đặt trực tiếp tại `wwwroot/lib/microsoft-signalr/signalr.min.js`, không phụ thuộc Internet / CDN bên ngoài.
   - Xem trước SLA tự động thời gian thực (`sla-calculator.js`) ngay trên biểu mẫu tạo ticket.
   - Bộ chỉ báo hạn cam kết SLA (`_SlaBadge.cshtml`) cảnh báo trực quan quá hạn / sắp hết hạn.

---

## 2. ÁNH XẠ CHI TIẾT 20 USE CASES (UC01 — UC20)

| Mã UC | Tên Use Case | Phân vùng (Area) | Controller & Action | Mô tả triển khai nghiệp vụ |
|---|---|---|---|---|
| **UC01** | Đăng nhập hệ thống | `Auth` | `AuthController.Login` | Xác thực người dùng bằng Cookie Auth, bảo vệ trước tài khoản Inactive (TC-01), phát hành claims danh tính. |
| **UC02** | Đổi mật khẩu | `Auth` | `AuthController.ChangePassword` | Cho phép người dùng thay đổi mật khẩu hiện tại, kiểm tra độ an toàn mật khẩu mới, băm BCrypt. |
| **UC03** | Đăng xuất | `Auth` | `AuthController.Logout` | Xóa Authentication Cookie, hủy phiên làm việc an toàn. |
| **UC04** | Tạo yêu cầu hỗ trợ | `User` | `TicketController.Create` | Khởi tạo Ticket ở trạng thái `New`, tính hạn SLA `DueAt` tự động, lưu tệp đính kèm an toàn, kích hoạt Domain Event thông báo Leader (TC-03). |
| **UC05** | Xem danh sách ticket cá nhân | `User` | `TicketController.Index` | BOLA Protection: Chỉ truy vấn danh sách ticket do chính `CurrentUserId` tạo, hỗ trợ lọc theo trạng thái và tìm kiếm từ khóa. |
| **UC06** | Xem chi tiết ticket | `User`, `Staff`, `Leader` | `TicketController.Details`, `StaffTicketController.Details`, `LeaderTicketController.Details` | Hiển thị thông tin ticket, đính kèm, lịch sử trạng thái, hộp thảo luận SignalR. Kiểm soát BOLA cho Staff và User (TC-07). |
| **UC07** | Bình luận & trao đổi real-time | `User`, `Staff`, `Leader` | `TicketController.AddComment`, `TicketHub` | Kiểm tra điều kiện `CanComment()` từ State Pattern (TC-08). Broadcast real-time qua SignalR tới phòng `ticket_{id}`. |
| **UC08** | Đóng ticket & Đánh giá | `User` | `TicketController.CloseAndRate` | Cho phép User đóng ticket đang ở trạng thái `Resolved`, ghi nhận đánh giá 1-5 sao và phản hồi vào bảng `Feedbacks` (TC-09). |
| **UC09** | Mở lại yêu cầu (Reopen) | `User` | `TicketController.Reopen` | Cho phép User mở lại ticket `Resolved` nếu chưa hài lòng, chuyển trạng thái về `InProgress` để kỹ thuật viên tiếp tục xử lý. |
| **UC10** | Xem hàng đợi được phân công | `Staff` | `StaffTicketController.Assigned` | Kỹ thuật viên xem danh sách các yêu cầu được Leader phân công trực tiếp cho mình, kèm cảnh báo SLA khẩn cấp. |
| **UC11** | Tiếp nhận xử lý sự cố | `Staff` | `StaffTicketController.StartProgress` | Chuyển trạng thái từ `Assigned` sang `InProgress` thông qua State Pattern (TC-05). |
| **UC12** | Chờ phản hồi bên ngoài | `Staff` | `StaffTicketController.PutOnHold` | Chuyển trạng thái sang `Waiting` khi chờ linh kiện hoặc thông tin từ bên thứ ba. |
| **UC13** | Giải quyết sự cố | `Staff` | `StaffTicketController.Resolve` | Chuyển trạng thái sang `Resolved`, ghi nhận thời gian khắc phục sự cố `ResolvedAt` để đánh giá SLA. |
| **UC14** | Phân công kỹ thuật viên | `Leader` | `LeaderTicketController.Assign` | Leader tiếp nhận ticket ở trạng thái `New`, chọn kỹ thuật viên phù hợp, chuyển trạng thái sang `Assigned` (TC-04). |
| **UC15** | Từ chối yêu cầu không hợp lệ | `Leader` | `LeaderTicketController.Reject` | Leader từ chối ticket `New` với lý do cụ thể, chuyển sang `Rejected`. Chặn từ chối ticket `InProgress` (TC-06). |
| **UC16** | Phân công lại nhân sự (Reassign) | `Leader` | `LeaderTicketController.Reassign` | Điều chuyển kỹ thuật viên xử lý khi nhân viên ban đầu quá tải hoặc vắng mặt; thêm ghi chú nội bộ kỹ thuật. |
| **UC17** | Dashboard phân tích SLA | `Leader` | `LeaderDashboardController.Index` | Biểu đồ và chỉ số KPI: Tỷ lệ tuân thủ SLA (Compliance Rate), tải công việc từng nhân viên, phân bố danh mục sự cố. |
| **UC18** | Quản lý người dùng | `Admin` | `UserController` (Index, Create, Edit, ToggleStatus, ResetPassword) | CRUD tài khoản, kích hoạt/vô hiệu hóa (ngăn tự khóa tài khoản Admin - TC-10), đặt lại mật khẩu người dùng. |
| **UC19** | Quản lý vai trò & RBAC Matrix | `Admin` | `RoleController.Index` | Ma trận quyền hạn chi tiết trên 20 Use Cases cho 4 vai trò, thống kê số lượng tài khoản theo vai trò. |
| **UC20** | Nhật ký kiểm toán hệ thống | `Admin` | `AuditController.Index`, `AuditController.Details` | Truy vấn và lọc dữ liệu `SystemLogs` theo hành động, người dùng, thời gian. Xem chi tiết payload JSON qua Modal. |

---

## 3. ĐỐI CHIẾU 10 KỊCH BẢN KIỂM NGHIỆM TẠI BẢNG 7.2 (BÁO CÁO ĐỒ ÁN)

| Kịch bản (Test Case) | Yêu cầu kiểm nghiệm | Cơ chế xử lý trong .NET 9 | Kết quả kiểm thử |
|---|---|---|---|
| **TC-01** | Tài khoản bị vô hiệu hóa (Inactive) không thể đăng nhập. | `AuthController.Login` kiểm tra `user.Status != UserStatus.Active`, từ chối phát hành Cookie, ghi nhận Audit Log cảnh báo bảo mật. | **ĐẠT (PASS)** |
| **TC-02** | Người dùng truy cập đường dẫn trái vai trò nhận mã lỗi HTTP 403 Forbidden. | `AuthorizeRolesAttribute` chặn truy cập tại ActionFilter pipeline, chuyển hướng sang `/AccessDenied` hoặc trả về HTTP 403. | **ĐẠT (PASS)** |
| **TC-03** | Tạo yêu cầu mới khởi tạo trạng thái `New`, tự động tính `DueAt` và gửi thông báo tới Leader. | `Ticket.Create()` sinh ticket với `StatusId = New`, SLA được tính theo `Priority`, kích hoạt `TicketCreatedEvent` tới nhóm `role_Leader`. | **ĐẠT (PASS)** |
| **TC-04** | Phân công yêu cầu chuyển trạng thái sang `Assigned`, gửi thông báo cho Staff và User. | State Pattern chuyển trạng thái qua `ticket.Assign(staffId, leaderId)`, phát Domain Event tới nhóm `user_{staffId}` và `user_{creatorId}`. | **ĐẠT (PASS)** |
| **TC-05** | Chuyển đổi trạng thái không hợp lệ bị từ chối bằng `Result<T>.Failure`. | Lớp trạng thái cụ thể của State Pattern chặn chuyển đổi trái phép (ví dụ: Staff không thể chuyển trực tiếp từ `New` sang `Closed`). | **ĐẠT (PASS)** |
| **TC-06** | Leader không thể từ chối (Reject) yêu cầu đang ở trạng thái `InProgress`. | `InProgressTicketState.Reject()` trả về lỗi `INVALID_STATE_TRANSITION`, giao diện ẩn nút từ chối. | **ĐẠT (PASS)** |
| **TC-07** | Kỹ thuật viên không thể truy cập yêu cầu không được phân công cho mình (BOLA Defense). | `StaffTicketController.Details` và `TicketHub.JoinTicketGroup` đối chiếu `ticket.AssignedToId == CurrentUserId`, trả về 403/HubException nếu không khớp. | **ĐẠT (PASS)** |
| **TC-08** | Chặn bình luận đối với các yêu cầu đã ở trạng thái `Resolved`, `Closed`, `Rejected`. | Kiểm tra `ticket.CanComment()` tại Controller và Hub. Giao diện `ticket-realtime.js` khóa form nhập và hiển thị thông báo trạng thái. | **ĐẠT (PASS)** |
| **TC-09** | Đóng và đánh giá yêu cầu đã giải quyết chuyển trạng thái sang `Closed`, lưu bản ghi Feedback. | `TicketController.CloseAndRate` thực thi `ticket.Close(feedback, user)`, lưu sao đánh giá và nội dung phản hồi đồng bộ trong Transaction. | **ĐẠT (PASS)** |
| **TC-10** | Quản trị viên có thể bật/tắt tài khoản người khác nhưng không thể tự vô hiệu hóa tài khoản của chính mình. | `UserController.ToggleStatus` kiểm tra `id == CurrentUserId`, trả về lỗi nghiệp vụ ngăn chặn Admin tự khóa tài khoản của bản thân. | **ĐẠT (PASS)** |

---

## 4. DANH MỤC TỆP NGUỒN ĐÃ TẠO VÀ CẬP NHẬT

### 4.1. Real-time & SignalR
- `src/ItTms.Web/Hubs/TicketHub.cs`
- `src/ItTms.Web/Services/SignalRNotificationDispatcher.cs`
- `src/ItTms.Web/Models/Realtime/CommentDto.cs`
- `src/ItTms.Web/Models/Realtime/NotificationDto.cs`
- `src/ItTms.Web/Models/Realtime/TicketStatusChangedDto.cs`
- `src/ItTms.Web/wwwroot/lib/microsoft-signalr/signalr.min.js` (47,668 bytes - local)
- `src/ItTms.Web/wwwroot/lib/microsoft-signalr/signalr.js` (156,695 bytes - local)
- `src/ItTms.Web/wwwroot/js/ticket-realtime.js`
- `src/ItTms.Web/wwwroot/js/signalr-notification.js`
- `src/ItTms.Web/wwwroot/js/sla-calculator.js`

### 4.2. CurrentUserService & Controllers
- `src/ItTms.Web/Services/CurrentUserService.cs`
- `src/ItTms.Web/Controllers/BaseController.cs`
- `src/ItTms.Web/Controllers/HomeController.cs`
- `src/ItTms.Web/Areas/Auth/Controllers/AuthBaseController.cs`
- `src/ItTms.Web/Areas/Auth/Controllers/AuthController.cs`
- `src/ItTms.Web/Areas/User/Controllers/UserBaseController.cs`
- `src/ItTms.Web/Areas/User/Controllers/DashboardController.cs`
- `src/ItTms.Web/Areas/User/Controllers/TicketController.cs`
- `src/ItTms.Web/Areas/Staff/Controllers/StaffBaseController.cs`
- `src/ItTms.Web/Areas/Staff/Controllers/DashboardController.cs`
- `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs`
- `src/ItTms.Web/Areas/Leader/Controllers/LeaderBaseController.cs`
- `src/ItTms.Web/Areas/Leader/Controllers/LeaderDashboardController.cs`
- `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs`
- `src/ItTms.Web/Areas/Admin/Controllers/AdminBaseController.cs`
- `src/ItTms.Web/Areas/Admin/Controllers/AdminDashboardController.cs`
- `src/ItTms.Web/Areas/Admin/Controllers/UserController.cs`
- `src/ItTms.Web/Areas/Admin/Controllers/RoleController.cs`
- `src/ItTms.Web/Areas/Admin/Controllers/AuditController.cs`

### 4.3. ViewModels
- `src/ItTms.Web/Models/ViewModels/AuthViewModels.cs`
- `src/ItTms.Web/Models/ViewModels/TicketViewModels.cs`
- `src/ItTms.Web/Models/ViewModels/DashboardViewModels.cs`
- `src/ItTms.Web/Models/ViewModels/AdminViewModels.cs`

### 4.4. Razor Views & Layouts
- `src/ItTms.Web/Views/Shared/_Layout.cshtml`
- `src/ItTms.Web/Views/Shared/_SvgIcons.cshtml`
- `src/ItTms.Web/Views/Shared/_ToastAlerts.cshtml`
- `src/ItTms.Web/Views/Shared/_StatusBadge.cshtml`
- `src/ItTms.Web/Views/Shared/_SlaBadge.cshtml`
- `src/ItTms.Web/Views/Shared/AccessDenied.cshtml`
- `src/ItTms.Web/Views/Home/Index.cshtml`
- `src/ItTms.Web/Areas/Auth/Views/Auth/Login.cshtml`
- `src/ItTms.Web/Areas/Auth/Views/Auth/ChangePassword.cshtml`
- `src/ItTms.Web/Areas/User/Views/Dashboard/Index.cshtml`
- `src/ItTms.Web/Areas/User/Views/Ticket/Create.cshtml`
- `src/ItTms.Web/Areas/User/Views/Ticket/Index.cshtml`
- `src/ItTms.Web/Areas/User/Views/Ticket/Details.cshtml`
- `src/ItTms.Web/Areas/Staff/Views/Dashboard/Index.cshtml`
- `src/ItTms.Web/Areas/Staff/Views/StaffTicket/Assigned.cshtml`
- `src/ItTms.Web/Areas/Staff/Views/StaffTicket/Details.cshtml`
- `src/ItTms.Web/Areas/Staff/Views/StaffTicket/History.cshtml`
- `src/ItTms.Web/Areas/Leader/Views/LeaderDashboard/Index.cshtml`
- `src/ItTms.Web/Areas/Leader/Views/LeaderTicket/NewQueue.cshtml`
- `src/ItTms.Web/Areas/Leader/Views/LeaderTicket/Index.cshtml`
- `src/ItTms.Web/Areas/Leader/Views/LeaderTicket/Details.cshtml`
- `src/ItTms.Web/Areas/Admin/Views/AdminDashboard/Index.cshtml`
- `src/ItTms.Web/Areas/Admin/Views/User/Index.cshtml`
- `src/ItTms.Web/Areas/Admin/Views/User/Create.cshtml`
- `src/ItTms.Web/Areas/Admin/Views/User/Edit.cshtml`
- `src/ItTms.Web/Areas/Admin/Views/Role/Index.cshtml`
- `src/ItTms.Web/Areas/Admin/Views/Audit/Index.cshtml`

### 4.5. Tests Bổ sung cho M3
- `tests/ItTms.Tests/UnitTests/Web/CurrentUserServiceTests.cs` (4 unit tests)
- `tests/ItTms.Tests/UnitTests/Web/TicketHubTests.cs` (6 unit tests)

---

## 5. KẾT QUẢ BIÊN DỊCH VÀ KIỂM THỬ TỔNG THỂ

1. **Biên dịch mã nguồn**:
   - Lệnh thực thi: `dotnet build c:\IT_System\ItTms.sln`
   - Kết quả: `Build succeeded. 0 Warning(s), 0 Error(s)`
   - Thời gian biên dịch: 6.26 giây

2. **Thực thi bộ kiểm thử toàn diện**:
   - Lệnh thực thi: `dotnet test c:\IT_System\ItTms.sln`
   - Kết quả: `Passed! - Failed: 0, Passed: 292, Skipped: 0, Total: 292, Duration: 25 s - ItTms.Tests.dll (net9.0)`
   - Tỷ lệ vượt qua: **100% (292/292 tests passed)**
   - Không phát sinh bất kỳ regression hay lỗi suy giảm nào so với baseline.
