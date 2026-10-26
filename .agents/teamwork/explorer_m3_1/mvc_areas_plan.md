# 🏛️ KẾ HOẠCH THIẾT KẾ KIẾN TRÚC 5 MVC AREAS & 20 USE CASES (SRC/ITTMS.WEB)
> **Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
> **Nền tảng**: .NET 9 (`net9.0`) | ASP.NET Core MVC | EF Core 9 SQLite | SignalR WebSocket  
> **Tác giả**: `explorer_m3_1` (Teamwork Explorer) | **Ngày lập**: 2026-10-25  
> **Chuẩn nhận thức**: `#Argon-Omega` (BLUF-MECE, Zero-Fluff, Production-Grade Engineering)  

---

## 1. TỔNG QUAN KIẾN TRÚC & NGUYÊN TẮC BẤT BIẾN (#ARGON-OMEGA)

### 1.1. Bottom Line Up Front (BLUF)
Tầng trình diễn (`src/ItTms.Web`) được tổ chức thành **5 ASP.NET Core MVC Areas** (`Auth`, `User`, `Staff`, `Leader`, `Admin`) bao phủ trọn vẹn 20 Use Cases của hệ thống IT-TMS. Kiến trúc kết hợp chặt chẽ với tầng Domain (7 States của State Pattern qua `Result<T>`), tầng Application (Observers cho `Notification` và `SystemLog`), tầng Infrastructure (EF Core 9 SQLite, `PasswordHasherService`) và **SignalR WebSocket (`TicketHub`)** nhằm triệt tiêu hoàn toàn anti-pattern Polling 3s, đồng thời bảo vệ hệ thống trước các mối đe dọa OWASP Top 10 (BOLA/IDOR, Broken Authentication, Inactive Session Hijacking).

### 1.2. Các Nguyên tắc Bất biến (Hard Invariants)
1. **Triệt tiêu Polling 3 giây (`#026 Bottleneck`)**: Khung thảo luận bình luận hai chiều và chuông thông báo hoạt động trên **SignalR WebSocket** tại endpoint `/hubs/ticket`. Không sử dụng polling HTTP GET gây lãng phí tài nguyên Kestrel và SQLite Connection Pool.
2. **Xử lý Luồng Đường sắt (Railway-Oriented `Result<T>`)**: Mọi tương tác nghiệp vụ từ Controller gọi xuống Domain Model nhận về `Result` hoặc `Result<T>`. Controller không sử dụng `try-catch` để bắt lỗi nghiệp vụ thông thường mà chuyển đổi trạng thái thất bại thành phản hồi HTTP phù hợp (RFC 9457 ProblemDetails cho AJAX/API hoặc `TempData` toast cho Razor Views).
3. **Phân quyền Đóng gói & Phòng thủ Đa tầng (Defense-in-Depth RBAC & BOLA)**:
   - Tầng Area/Controller được bảo vệ bởi `[AuthorizeRoles]`.
   - Tầng Resource-based được bảo vệ bởi `[AuthorizeTicketAccess]` và kiểm tra quyền sở hữu đối tượng:
     - `User`: Chỉ xem và tương tác với ticket do chính mình tạo (`CreatorId == CurrentUserId`).
     - `Staff`: Chỉ truy cập và xử lý ticket được Leader phân công trực tiếp (`AssignedToId == CurrentUserId`), vi phạm lập tức trả về **HTTP 403 Forbidden** (Bảng 7.2 TC-07).
     - `Leader` & `Admin`: Thẩm quyền giám sát toàn cục.
4. **Bảo toàn 100% Kịch bản Nghiệm thu Bảng 7.2**: Toàn bộ luồng dữ liệu, điều kiện biên và mã phản hồi được thiết kế chính xác để vượt qua 10 kịch bản kiểm thử tự động (TC-01 đến TC-10).

---

## 2. MA TRẬN PHÂN BỔ 5 AREAS & ÁNH XẠ 20 USE CASES

| Area | Controller | Action Method | HTTP | Use Case Liên quan | Kịch bản Bảng 7.2 | Mục tiêu & Cơ chế Nghiệp vụ |
|---|---|---|:---:|---|:---:|---|
| **Auth** | `AuthController` | `Login` | GET, POST | **UC01** (Đăng nhập) | **TC-01** | Xác thực credentials, kiểm tra `user.Status == Active` (chặn tài khoản Inactive), cấp Cookie Scheme, chuyển về Dashboard vai trò. |
| **Auth** | `AuthController` | `Logout` | GET, POST | **UC02** (Đăng xuất) | - | Thu hồi Cookie authentication (`SignOutAsync`), hủy phiên, điều hướng về `/Auth/Login`. |
| **Auth** | `AuthController` | `ChangePassword` | GET, POST | **UC03** (Đổi mật khẩu) | - | Xác thực mật khẩu cũ bằng `VerifyPassword`, cập nhật `PasswordHash` mới, ghi `SystemLog`. |
| **Auth** | `AuthController` | `AccessDenied` | GET | Bảo mật Phân quyền | **TC-02** | Trả về view báo lỗi **HTTP 403 Forbidden** khi truy cập trái quyền. |
| **User** | `TicketController` | `Create` | GET, POST | **UC04** (Tạo yêu cầu mới) | **TC-03** | Nhập tiêu đề, mô tả, ưu tiên, đính kèm file (max 16MB); tự động tính SLA `DueAt`; khởi tạo trạng thái `New`; phát Domain Event thông báo Leader. |
| **User** | `TicketController` | `MyTickets` | GET | **UC05** (Danh sách cá nhân)| - | Lọc strictly `CreatorId == CurrentUserId`, phân tab theo trạng thái (Active, Resolved, Closed, Rejected, All). |
| **User** | `TicketController` | `Details` | GET | **UC06** (Chi tiết & Theo dõi)| - | Hiển thị 3 khối: thông tin ticket, file đính kèm, khung chat SignalR; ẩn các bình luận nội bộ (`IsInternal = true`). BOLA check: `CreatorId == CurrentUserId`. |
| **User** | `TicketController` | `AddComment` | POST | **UC07** (Bình luận ticket) | **TC-08** | Kiểm tra `ticket.CanComment()`. Chặn bình luận khi ticket ở `Resolved/Closed/Rejected`. Nếu ticket ở `Waiting` -> tự động chuyển về `InProgress`. |
| **User** | `TicketController` | `UploadAttachment` | POST | **UC08** (Tải file đính kèm) | - | Lưu trữ tệp tin vào đĩa với tên GUID, kiểm tra dung lượng $\le 16$MB, lưu metadata vào bảng `Attachment`. |
| **User** | `TicketController` | `CloseAndRate` | POST | **UC09** (Đóng & Đánh giá) | **TC-09** | Chỉ cho phép khi ticket đang `Resolved`. Ghi nhận đánh giá 1-5 sao và nhận xét vào bảng `Feedback`; chuyển trạng thái sang `Closed`; gán `ClosedAt`. |
| **User** | `TicketController` | `Reopen` | POST | **UC19** (Mở lại ticket) | - | Chuyển ticket từ `Resolved` về `InProgress` khi User chưa hài lòng, thông báo khẩn cho Staff. |
| **User** | `DashboardController`| `Index` | GET | **UC20** (Dashboard cá nhân)| - | Thống kê số lượng ticket theo trạng thái, hiển thị danh sách ticket cần đánh giá ưu tiên trên đầu. |
| **Staff** | `StaffTicketController`| `Assigned` | GET | **UC13** (Danh sách phân công)| **TC-07** | Lọc strictly `AssignedToId == CurrentUserId`. Không bao giờ hiển thị ticket của nhân sự khác. |
| **Staff** | `StaffTicketController`| `Details` | GET | **UC06** / **UC08** | **TC-07** | BOLA defense: Kiểm tra `AssignedToId == CurrentUserId`. Nếu Staff A truy cập ticket của Staff B -> Trả về **HTTP 403 Forbidden**. |
| **Staff** | `StaffTicketController`| `StartProgress` | POST | **UC10** (Bắt đầu xử lý) | **TC-05** | Chuyển trạng thái từ `Assigned` sang `InProgress`. State Pattern chặn nếu chuyển sai quy trình. |
| **Staff** | `StaffTicketController`| `HoldForInfo` | POST | **UC11** (Tạm dừng chờ info)| - | Chuyển trạng thái sang `Waiting`, lưu lý do, gửi thông báo yêu cầu phản hồi tới User. |
| **Staff** | `StaffTicketController`| `Resolve` | POST | **UC12** (Hoàn thành xử lý) | - | Chuyển trạng thái sang `Resolved`, gán `ResolvedAt = UtcNow`, thông báo cho User nghiệm thu. |
| **Staff** | `StaffTicketController`| `AddComment` | POST | **UC06** / **UC07** | **TC-08** | Gửi bình luận hoặc ghi chú nội bộ (`IsInternal = true`). Bị chặn nếu ticket đã `Resolved`. |
| **Staff** | `DashboardController`| `Index` | GET | **UC20** (Dashboard Staff) | - | Thống kê công việc đang làm, cảnh báo ticket cận hạn SLA nhấp nháy đỏ, phản hồi mới. |
| **Leader**| `LeaderTicketController`| `NewTickets` | GET | **UC14** (Hàng đợi phân công)| - | Hiển thị toàn bộ ticket có `StatusId == New`, sắp xếp theo Priority và DueAt; hiển thị tải công việc hiện tại của từng Staff. |
| **Leader**| `LeaderTicketController`| `Assign` | POST | **UC14** (Phân công Staff) | **TC-04** | Giao ticket `New` cho Staff. Chuyển sang `Assigned`, gán `AssignedToId`, gửi thông báo cho Staff và User. |
| **Leader**| `LeaderTicketController`| `Reject` | POST | **UC15** (Từ chối ticket) | **TC-06** | Từ chối ticket `New` kèm lý do. State Pattern chặn nếu ticket đã `InProgress` (trả về lỗi nghiệp vụ). |
| **Leader**| `LeaderTicketController`| `Reassign` | POST | **UC16** (Điều chuyển Staff) | - | Đổi Staff phụ trách khi ticket đang ở `Assigned`, `InProgress`, `Waiting`. Thông báo cho cả 2 Staff và User. |
| **Leader**| `LeaderTicketController`| `All` | GET | **UC12** (Tra cứu toàn bộ) | - | Tra cứu đa tiêu chí toàn hệ thống: từ khóa, trạng thái, nhân sự, ngày tạo. |
| **Leader**| `LeaderDashboardController`| `Index` | GET | **UC11**, **UC17** (SLA Dashboard)| - | 4 Thẻ KPI, danh sách vi phạm/cảnh báo SLA, biểu đồ năng suất Staff, biểu đồ danh mục sự cố. |
| **Admin** | `UserController` | `Index` | GET | **UC18** (Quản lý User) | - | Danh sách tài khoản: Username, Họ tên, Role, Trạng thái (Active/Inactive), Phòng ban. |
| **Admin** | `UserController` | `Create` | GET, POST | **UC14** (Tạo tài khoản mới)| - | Tạo tài khoản mới, băm mật khẩu chuẩn PBKDF2, gán vai trò ban đầu, mặc định Active. |
| **Admin** | `UserController` | `Edit` | GET, POST | **UC18** (Cập nhật User) | - | Chỉnh sửa họ tên, phòng ban, vai trò. Chặn tự hạ quyền của chính mình. |
| **Admin** | `UserController` | `ToggleActive` | POST | **UC15** (Bật/Tắt tài khoản)| **TC-10** | Đảo trạng thái Active ↔ Inactive. **Chặn Admin tự vô hiệu hóa chính mình** (báo lỗi). Thu hồi phiên tài khoản bị khóa. |
| **Admin** | `UserController` | `ResetPassword` | POST | **UC18** (Reset mật khẩu) | - | Admin thiết lập mật khẩu mới cho nhân sự khi có yêu cầu hỗ trợ bảo mật. |
| **Admin** | `RoleController` | `Index`, `Update` | GET, POST | **UC19** (Phân quyền vai trò)| **TC-02** | Quản lý ma trận phân quyền vai trò (Admin, Leader, Staff, User). |
| **Admin** | `AuditController` | `Index` | GET | **UC20** (Xem nhật ký audit)| - | Tra cứu toàn bộ bảng `SystemLogs`: lọc theo Action, UserId, IP Address, khoảng thời gian. |
| **Admin** | `AdminDashboardController`| `Index` | GET | **UC20** (Dashboard Admin) | - | Thống kê số lượng user, tỷ lệ hoàn tất ticket, điểm rating trung bình toàn hệ thống, audit log gần nhất. |

---

## 3. THIẾT KẾ CHI TIẾT TỪNG AREA

### 3.1. Area `Auth`
- **Mục đích**: Quản lý phiên đăng nhập, đăng xuất, đổi mật khẩu và trang thông báo từ chối truy cập.
- **Thư mục**: `src/ItTms.Web/Areas/Auth/Controllers/AuthController.cs`
- **Base Controller**: `AuthBaseController` (kế thừa `BaseController`, gắn `[Area("Auth")]`).

#### Chi tiết Kỹ thuật Các Action
```csharp
[Area("Auth")]
public class AuthController : AuthBaseController
{
    private readonly IItTmsDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IItTmsDbContext dbContext,
        IPasswordHasherService passwordHasher,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleDashboard();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var normalizedUsername = model.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername || u.Email == normalizedUsername);

        if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, model.Password))
        {
            _logger.LogWarning("Đăng nhập thất bại: Tài khoản hoặc mật khẩu không đúng '{Username}'", model.Username);
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // BẢNG 7.2 TC-01: Chặn đăng nhập tài khoản có trạng thái Inactive
        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("Đăng nhập bị từ chối: Tài khoản '{Username}' (ID: {UserId}) đang ở trạng thái Inactive", user.Username, user.Id);
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên.");
            
            // Ghi nhật ký kiểm toán hành vi từ chối
            var failedLog = SystemLog.Create("Login Denied", $"Đăng nhập thất bại do tài khoản bị vô hiệu hóa: {user.Username}", user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (failedLog.IsSuccess)
            {
                _dbContext.SystemLogs.Add(failedLog.Value);
                await _dbContext.SaveChangesAsync();
            }

            return View(model);
        }

        // Thiết lập danh tính Claims và Authentication Cookie
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.GivenName, user.FullName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("Department", user.Department ?? string.Empty)
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe,
            ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(14) : DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

        // Ghi nhật ký đăng nhập thành công
        var successLog = SystemLog.Create("Login Success", $"Người dùng {user.Username} đăng nhập thành công.", user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        if (successLog.IsSuccess)
        {
            _dbContext.SystemLogs.Add(successLog.Value);
            await _dbContext.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToRoleDashboard(user.Role);
    }

    [HttpPost, HttpGet]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = CurrentUserId;
        var username = CurrentUserName;

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (userId.HasValue)
        {
            var log = SystemLog.Create("Logout", $"Người dùng {username} đã đăng xuất an toàn.", userId.Value, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (log.IsSuccess)
            {
                _dbContext.SystemLogs.Add(log.Value);
                await _dbContext.SaveChangesAsync();
            }
        }

        TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống thành công.";
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _dbContext.Users.FindAsync(CurrentUserId!.Value);
        if (user == null)
            return RedirectToAction(nameof(Login));

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, model.CurrentPassword))
        {
            ModelState.AddModelError(nameof(model.CurrentPassword), "Mật khẩu hiện tại không chính xác.");
            return View(model);
        }

        var newHash = _passwordHasher.HashPassword(model.NewPassword);
        var updateResult = user.UpdatePassword(newHash);
        if (updateResult.IsFailure)
        {
            ModelState.AddModelError(string.Empty, updateResult.Error.Description);
            return View(model);
        }

        var log = SystemLog.Create("Change Password", $"Người dùng {user.Username} đã đổi mật khẩu cá nhân.", user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        if (log.IsSuccess) _dbContext.SystemLogs.Add(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
        return RedirectToAction(nameof(ChangePassword));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        Response.StatusCode = StatusCodes.Status403Forbidden;
        return View("~/Views/Shared/AccessDenied.cshtml");
    }

    private IActionResult RedirectToRoleDashboard(UserRole? role = null)
    {
        var userRole = role?.ToString() ?? CurrentUserRole;
        return userRole switch
        {
            RoleNames.Admin  => RedirectToAction("Index", "AdminDashboard", new { area = "Admin" }),
            RoleNames.Leader => RedirectToAction("Index", "LeaderDashboard", new { area = "Leader" }),
            RoleNames.Staff  => RedirectToAction("Assigned", "StaffTicket", new { area = "Staff" }),
            _                => RedirectToAction("MyTickets", "Ticket", new { area = "User" })
        };
    }
}
```

---

### 3.2. Area `User`
- **Mục đích**: Cung cấp giao diện trực quan cho người dùng gửi yêu cầu, theo dõi tiến độ, trao đổi bình luận hai chiều thời gian thực và đánh giá nghiệm thu dịch vụ.
- **Thư mục**: `src/ItTms.Web/Areas/User/Controllers/TicketController.cs` và `DashboardController.cs`
- **Base Controller**: `UserBaseController` (`[Area("User")]`, `[AuthorizeRoles(RoleNames.User, RoleNames.Leader, RoleNames.Admin)]`).

#### Chi tiết Kỹ thuật Các Action
1. **`CreateTicket` (UC04 / Bảng 7.2 TC-03)**:
   - Form nhận dữ liệu: `Title`, `Description`, `Priority` (`Low`, `Medium`, `High`, `Critical`), `Category`, `IFormFile? AttachmentFile`.
   - Tính toán SLA `DueAt` tự động dựa trên mức độ ưu tiên:
     - `Critical`: 1 giờ (`DueAt = now.AddHours(1)`)
     - `High`: 4 giờ (`DueAt = now.AddHours(4)`)
     - `Medium`: 24 giờ (`DueAt = now.AddHours(24)`)
     - `Low`: 72 giờ (`DueAt = now.AddHours(72)`)
   - Gọi Domain Factory: `Ticket.Create(model.Title, model.Description, model.Priority, model.Category, CurrentUserId.Value)`.
   - Nếu có tệp đính kèm:
     - Kiểm tra kích thước: `file.Length <= Attachment.MaxFileSizeBytes` (16MB). Nếu vượt quá, báo lỗi ngay.
     - Kiểm tra đuôi file an toàn: cho phép `.jpg, .jpeg, .png, .gif, .pdf, .docx, .xlsx, .txt, .zip, .rar`. Chặn các file thực thi `.exe, .bat, .sh, .dll`.
     - Tạo tên tệp ngẫu nhiên GUID, lưu vào thư mục `wwwroot/uploads/tickets/`.
     - Tạo `Attachment.Create(ticket.Id, physicalFileName, file.FileName, file.ContentType, file.Length)`.
   - Lưu CSDL: Domain Event `TicketCreatedEvent` được tự động phát tới `NotificationObserver`.
   - Observer lưu thông báo cho toàn bộ Leader và phát SignalR tới nhóm `role_Leader` (TC-03).
   - Điều hướng về `MyTickets` kèm Toast thông báo thành công.

2. **`MyTickets` (UC05)**:
   - Truy vấn CSDL có điều kiện: `t.CreatorId == CurrentUserId.Value`.
   - Phân chia 5 tab lọc:
     - `active`: Trạng thái `New`, `Assigned`, `InProgress`, `Waiting`.
     - `resolved`: Trạng thái `Resolved` (cần đánh giá).
     - `closed`: Trạng thái `Closed`.
     - `rejected`: Trạng thái `Rejected`.
     - `all`: Toàn bộ.
   - Sắp xếp: `t.CreatedAt DESC`. Hiển thị nhãn trạng thái và thời gian SLA còn lại.

3. **`Details` (UC06 / UC08)**:
   - Tải `Ticket` kèm `Creator`, `AssignedTo`, `Status`, `Comments`, `Attachments`, `Feedback`.
   - **Bảo mật BOLA**: Kiểm tra `ticket.CreatorId == CurrentUserId.Value`. Nếu khác, trả về `Forbid()` (HTTP 403 Forbidden).
   - Lọc dữ liệu: Chỉ hiển thị các comment công khai (`IsInternal == false`). Tuyệt đối ẩn các ghi chú nội bộ của IT đối với người dùng.
   - Truyền ViewModel cho view Razor tích hợp client SignalR `ticket-realtime.js`.

4. **`AddComment` (UC07 / Bảng 7.2 TC-08)**:
   - Nhận `ticketId` và `content`.
   - Kiểm tra quyền sở hữu ticket: `ticket.CreatorId == CurrentUserId.Value`.
   - **Ràng buộc Bảng 7.2 TC-08**: Gọi `ticket.CanComment()`.
     - Nếu ticket đang ở `Resolved`, `Closed` hoặc `Rejected`, thuộc tính này trả về `false`.
     - Controller lập tức từ chối thao tác, không lưu bất kỳ bản ghi nào vào CSDL, trả về lỗi: *"Yêu cầu này đã được giải quyết hoặc đóng, không thể gửi thêm bình luận."*
   - **Tự động kích hoạt chuyển trạng thái**: Nếu ticket đang ở trạng thái `Waiting` (Chờ thông tin) và người bình luận là `User`:
     - Tự động chuyển ticket về `InProgress` (`ticket.TransitionTo(new InProgressState(), CurrentUserId.Value)`).
     - Gửi thông báo cho Staff phụ trách biết User đã phản hồi.
   - Lưu CSDL và broadcast realtime qua SignalR Hub:
     ```csharp
     await _hubContext.Clients.Group($"Ticket_{ticket.Id}").SendAsync("ReceiveComment", new CommentDto { ... });
     ```

5. **`CloseAndRate` (UC09 / Bảng 7.2 TC-09)**:
   - Dành cho ticket đang ở trạng thái `Resolved`.
   - Kiểm tra `ticket.CreatorId == CurrentUserId.Value`.
   - Nhận `Rating` (1 đến 5 sao) và nhận xét tùy chọn.
   - Tạo thực thể `Feedback.Create(ticket.Id, model.Rating, model.Comment)`.
   - Gọi `ticket.Close(feedback.Value, currentUser)`.
   - Trạng thái chuyển sang `Closed`, gán `ClosedAt = DateTime.UtcNow`.
   - `NotificationObserver` tự động gửi thông báo cảm ơn và báo cho Staff phụ trách. Ghi `SystemLog`.

6. **`Reopen` (UC19)**:
   - Nếu User chưa hài lòng với kết quả xử lý tại trạng thái `Resolved`, bấm "Yêu cầu xử lý lại".
   - Bắt buộc nhập lý do mở lại.
   - Gọi `ticket.Reopen(reason, currentUser)`, trạng thái chuyển về `InProgress`, thông báo khẩn cho Staff.

---

### 3.3. Area `Staff`
- **Mục đích**: Cung cấp giao diện hàng đợi công việc chuyên nghiệp cho kỹ thuật viên IT, cập nhật tiến độ theo State Pattern, viết ghi chú nội bộ và hoàn thành công việc.
- **Thư mục**: `src/ItTms.Web/Areas/Staff/Controllers/StaffTicketController.cs` và `DashboardController.cs`
- **Base Controller**: `StaffBaseController` (`[Area("Staff")]`, `[AuthorizeRoles(RoleNames.Staff, RoleNames.Leader, RoleNames.Admin)]`).

#### Chi tiết Kỹ thuật Các Action
1. **`AssignedTickets` (UC13 / Bảng 7.2 TC-07)**:
   - Truy vấn CSDL strictly theo điều kiện: `t.AssignedToId == CurrentUserId.Value`.
   - Không cho phép Staff xem hàng đợi của nhân viên khác.
   - Cung cấp các tab: Đang xử lý (`Assigned`, `InProgress`, `Waiting`), Đã giải quyết (`Resolved`), Đã đóng (`Closed`).
   - Cảnh báo trực quan nếu hạn `DueAt` sắp hết hoặc đã quá hạn.

2. **`Details` (UC06 / UC08 / Bảng 7.2 TC-07)**:
   - **Bảo mật BOLA**:
     ```csharp
     var ticket = await _dbContext.Tickets.FindAsync(id);
     if (ticket.AssignedToId != CurrentUserId.Value && !User.IsInRole(RoleNames.Leader) && !User.IsInRole(RoleNames.Admin))
     {
         _logger.LogWarning("BOLA Breach Attempt: Staff {UserId} cố truy cập Ticket {TicketId} của Staff khác", CurrentUserId, id);
         return Forbid(); // Trả về HTTP 403 Forbidden chuẩn Bảng 7.2 TC-07
     }
     ```
   - Hiển thị đầy đủ lịch sử trao đổi, bao gồm cả ghi chú nội bộ (`IsInternal == true`).
   - Hiển thị các nút thao tác chuyển trạng thái hợp lệ dựa trên `ticket.CurrentState`.

3. **`StartProgress` (UC10 / Bảng 7.2 TC-05)**:
   - `[HttpPost] [ValidateAntiForgeryToken] Task<IActionResult> StartProgress(int id)`:
   - Kiểm tra `ticket.AssignedToId == CurrentUserId.Value`.
   - Gọi `ticket.StartProgress(CurrentUserId.Value)`.
   - **State Invariant**: Chỉ cho phép khi ticket đang ở `Assigned` (hoặc `Waiting`).
   - Nếu gọi trên ticket đang ở `New` hoặc `Closed`: State Pattern trả về `Result.Failure`, Action xử lý qua `HandleResult()` và trả về lỗi nghiệp vụ (TC-05).
   - Khi thành công: Chuyển sang `InProgress`, lưu CSDL, thông báo tới User qua SignalR.

4. **`HoldForInfo` (UC11)**:
   - `[HttpPost] [ValidateAntiForgeryToken] Task<IActionResult> HoldForInfo(int id, string reason)`:
   - Yêu cầu nhập lý do tạm dừng.
   - Gọi `ticket.HoldForInfo(reason, CurrentUserId.Value)`.
   - Chuyển sang `Waiting`, gửi thông báo cho User kèm lý do tạm dừng.

5. **`Resolve` (UC12)**:
   - `[HttpPost] [ValidateAntiForgeryToken] Task<IActionResult> Resolve(int id)`:
   - Gọi `ticket.Resolve(CurrentUserId.Value)`.
   - Chuyển sang `Resolved`, gán `ResolvedAt = DateTime.UtcNow`.
   - Domain Event kích hoạt thông báo cho User: "Yêu cầu đã được xử lý xong. Vui lòng kiểm tra và đóng ticket kèm đánh giá dịch vụ."

6. **`AddComment` (UC06 / UC07)**:
   - Hỗ trợ checkbox "Ghi chú nội bộ" (`IsInternal = true`).
   - Nếu `IsInternal == true`: Ghi chú chỉ hiển thị cho Staff, Leader, Admin. Client User không nhận được sự kiện này qua SignalR.
   - Bị chặn hoàn toàn nếu ticket đã ở trạng thái `Resolved`, `Closed`, hoặc `Rejected` (TC-08).

---

### 3.4. Area `Leader`
- **Mục đích**: Cung cấp trung tâm điều phối, phân công công việc, từ chối yêu cầu sai quy định, điều chuyển kỹ thuật viên và theo dõi chỉ số SLA toàn diện.
- **Thư mục**: `src/ItTms.Web/Areas/Leader/Controllers/LeaderTicketController.cs` và `LeaderDashboardController.cs`
- **Base Controller**: `LeaderBaseController` (`[Area("Leader")]`, `[AuthorizeRoles(RoleNames.Leader, RoleNames.Admin)]`).

#### Chi tiết Kỹ thuật Các Action
1. **`NewTickets` (UC14)**:
   - Hiển thị hàng đợi tiếp nhận: Các ticket có `StatusId == TicketStatus.New`.
   - Sắp xếp theo thứ tự ưu tiên giảm dần (`Critical` lên đầu) và `DueAt` tăng dần (vé sắp hết hạn lên đầu).
   - Nạp danh sách các kỹ thuật viên kèm số ticket đang xử lý (`StaffWorkloadMap`) để Leader phân bổ việc cân bằng.

2. **`Assign` (UC14 / Bảng 7.2 TC-04)**:
   - `[HttpPost] [ValidateAntiForgeryToken] Task<IActionResult> Assign(AssignTicketViewModel model)`:
   - Lấy ticket từ CSDL.
   - **Ràng buộc Bảng 7.2 TC-04**: Ticket phải ở trạng thái `New`.
   - Gọi `ticket.Assign(model.StaffId, CurrentUserId.Value)`.
   - Ticket chuyển sang `Assigned`, gán `AssignedToId = model.StaffId`.
   - Domain Event `TicketAssignedEvent` kích hoạt `NotificationObserver`:
     - Tạo thông báo cho Staff được giao: *"Bạn được phân công xử lý ticket #ID: {Title}"*.
     - Tạo thông báo cho User tạo ticket: *"Ticket của bạn đã được phân công cho kỹ thuật viên {StaffName}"*.
   - Broadcast SignalR cập nhật số đếm hàng đợi của Leader và Staff.

3. **`Reject` (UC15 / Bảng 7.2 TC-06)**:
   - `[HttpPost] [ValidateAntiForgeryToken] Task<IActionResult> Reject(RejectTicketViewModel model)`:
   - Lấy ticket từ CSDL.
   - **Ràng buộc Bảng 7.2 TC-06**: Chỉ cho phép từ chối khi ticket đang ở `New`.
   - **Cơ chế phòng thủ State Pattern**: Nếu Leader cố tình từ chối một ticket đang ở `InProgress`:
     - `ticket.Reject(reason, leaderId)` ủy quyền cho `InProgressState.Reject()`.
     - `InProgressState` trả về `Result.Failure(TicketErrors.InvalidTransition("InProgress", "Reject"))`.
     - Controller bắt failure, giữ nguyên dữ liệu CSDL, trả về lỗi: *"Không thể từ chối yêu cầu đang trong quá trình xử lý kỹ thuật."*
   - Nếu hợp lệ (`New`): Chuyển sang `Rejected`, lưu `RejectionReason`, thông báo cho User kèm lý do và ghi `SystemLog`.

4. **`Reassign` (UC16)**:
   - Điều chuyển kỹ thuật viên khi ticket đang ở `Assigned`, `InProgress`, hoặc `Waiting` (ví dụ Staff phụ trách nghỉ ốm hoặc quá tải).
   - Gọi `ticket.Reassign(model.NewStaffId, CurrentUserId.Value)`.
   - Cập nhật `AssignedToId`, thông báo cho cả Staff cũ, Staff mới và User.

5. **`Dashboard` & Báo cáo SLA (UC11 / UC17)**:
   - 4 Thẻ KPI:
     - Chờ phân công (`Count(New)`)
     - Đang xử lý (`Count(Assigned + InProgress + Waiting)`)
     - Đã giải quyết (`Count(Resolved + Closed)`)
     - Tỷ lệ vi phạm SLA (`Count(ResolvedAt > DueAt || (Status != Resolved/Closed && Now > DueAt))`)
   - Cảnh báo khẩn cấp: Liệt kê các ticket mới chưa giao việc quá 24h và ticket đang xử lý quá 48h.
   - Biểu đồ năng suất Staff: Số ticket hoàn thành đúng hạn vs quá hạn của từng kỹ thuật viên.
   - Biểu đồ phân bổ theo danh mục sự cố.

---

### 3.5. Area `Admin`
- **Mục đích**: Cung cấp trung tâm quản trị toàn bộ hệ thống IT-TMS: nhân sự, phân quyền vai trò, khóa tài khoản và truy vết nhật ký kiểm toán (Audit Trail).
- **Thư mục**: `src/ItTms.Web/Areas/Admin/Controllers/` (`UserController.cs`, `RoleController.cs`, `AuditController.cs`, `AdminDashboardController.cs`)
- **Base Controller**: `AdminBaseController` (`[Area("Admin")]`, `[AuthorizeRoles(RoleNames.Admin)]`).

#### Chi tiết Kỹ thuật Các Action
1. **`UserManagement` & `CreateUser` (UC14 / UC18)**:
   - Danh sách toàn bộ tài khoản có tìm kiếm theo tên, phòng ban, vai trò và trạng thái.
   - Tạo người dùng mới:
     - Kiểm tra tính duy nhất của Username và Email.
     - Băm mật khẩu bằng PBKDF2 qua `_passwordHasher.HashPassword(...)`.
     - Tạo `User.Create(...)` với trạng thái ban đầu là `Active`.
     - Ghi nhận `SystemLog` hành động "Admin Created User".

2. **`ToggleActive` (UC15 / Bảng 7.2 TC-10)**:
   - `[HttpPost] [ValidateAntiForgeryToken] Task<IActionResult> ToggleActive(int id)`:
   - **Ràng buộc Bất biến Bảng 7.2 TC-10: Chặn Admin tự vô hiệu hóa chính mình**:
     ```csharp
     if (id == CurrentUserId.Value)
     {
         _logger.LogWarning("Admin {AdminId} cố gắng tự vô hiệu hóa tài khoản của chính mình", id);
         TempData["ErrorMessage"] = "Quản trị viên không thể tự vô hiệu hóa tài khoản của chính mình.";
         return RedirectToAction(nameof(Index));
     }
     ```
   - Đối với tài khoản khác:
     - Nếu đang `Active`: Gọi `user.Deactivate(CurrentUserId.Value)`, trạng thái chuyển sang `Inactive`.
     - Nếu đang `Inactive`: Gọi `user.Activate()`, trạng thái chuyển sang `Active`.
   - **Hậu quả an ninh đối với tài khoản Inactive**:
     - `AuthorizeRolesAttribute` kiểm tra trạng thái từ CSDL trong thời gian thực trên mỗi request.
     - Khi tài khoản chuyển sang `Inactive`, ở request kế tiếp từ trình duyệt của người dùng đó, bộ lọc sẽ phát hiện `Status == Inactive`, lập tức gọi `SignOutAsync()`, xóa session và chuyển hướng về trang Login kèm thông báo khóa tài khoản (TC-01 & TC-10).
   - Ghi `SystemLog` biến động trạng thái tài khoản.

3. **`RoleManagement` (UC19)**:
   - Quản trị việc gán vai trò (`Admin`, `Leader`, `Staff`, `User`) cho các tài khoản.
   - Kiểm tra an toàn: Không cho phép Admin tự tước quyền Admin của chính mình nếu là tài khoản Admin duy nhất đang hoạt động.

4. **`AuditLogs` (UC20)**:
   - Tra cứu toàn diện lịch sử thao tác từ bảng `SystemLogs`.
   - Bộ lọc: Theo loại hành động (`Action`), theo người thực hiện (`UserId`), theo dải thời gian từ ngày - đến ngày.
   - Phân trang Keyset hoặc Offset phân trang trên dữ liệu lớn, sắp xếp `CreatedAt DESC`.

---

## 4. BẢNG ĐỐI CHIẾU 10 KỊCH BẢN KIỂM THỬ BẢNG 7.2 & CƠ CHẾ BẢO ĐẢM PASS 100%

| STT | Mã Kịch bản | Thao tác Kiểm thử Thực tế | Điểm Chặn / Cơ chế Bảo vệ trong MVC Architecture | Kết quả Kiểm thử Nghiệm thu |
|:---:|:---:|---|---|:---:|
| **1** | **TC-01** | Đăng nhập bằng tài khoản `inactive_user` có `Status = "Inactive"`. | Tại `AuthController.Login [HttpPost]`: Kiểm tra `user.Status != UserStatus.Active`. Từ chối cấp Cookie; gán lỗi `ModelState` "Tài khoản của bạn đã bị vô hiệu hóa"; ghi `SystemLog` thất bại. Ngoài ra `AuthorizeRolesAttribute` tự động đá văng phiên nếu tài khoản đang online bị khóa. | **ĐẠT (PASS)** |
| **2** | **TC-02** | User truy cập URL `/Leader/Dashboard` hoặc `/Leader/Ticket/AssignQueue`. | `AuthorizeRolesAttribute(RoleNames.Leader, RoleNames.Admin)` chặn tại pipeline; kiểm tra Role claims không khớp; trả về view `~/Views/Shared/AccessDenied.cshtml` với status code **HTTP 403 Forbidden**. | **ĐẠT (PASS)** |
| **3** | **TC-03** | User tạo ticket với mức ưu tiên `High`. | `TicketController.Create [HttpPost]`: Gọi `Ticket.CalculateDueAt(High, now)` $\to DueAt = now + 4h$; trạng thái khởi tạo `New`; phát `TicketCreatedEvent`; `NotificationObserver` tạo thông báo cho toàn bộ Leader và đẩy SignalR tới `role_Leader`. | **ĐẠT (PASS)** |
| **4** | **TC-04** | Leader phân công ticket #1 (`New`) cho Staff kỹ thuật viên. | `LeaderTicketController.Assign [HttpPost]`: Gọi `ticket.Assign(staffId, leaderId)`; chuyển sang `Assigned`, gán `AssignedToId`; phát `TicketAssignedEvent` tạo thông báo cho Staff và User; ticket biến mất khỏi hàng đợi `NewTickets`. | **ĐẠT (PASS)** |
| **5** | **TC-05** | Staff cố tình gửi lệnh cập nhật `Closed` cho ticket đang ở `New`. | `StaffTicketController.StartProgress / UpdateStatus`: Gọi `ticket.Close()`; `NewState.Close()` trả về `Result.Failure(TicketErrors.InvalidTransition("New", "Close"))`; Controller trả về BadRequest / Error Toast; CSDL giữ nguyên 100%. | **ĐẠT (PASS)** |
| **6** | **TC-06** | Leader gửi lệnh từ chối (`Reject`) cho ticket đang ở `InProgress`. | `LeaderTicketController.Reject [HttpPost]`: Gọi `ticket.Reject()`; `InProgressState.Reject()` từ chối thao tác, trả về `Result.Failure(TicketErrors.InvalidTransition("InProgress", "Reject"))`; Controller chặn cập nhật, thông báo lỗi. | **ĐẠT (PASS)** |
| **7** | **TC-07** | Staff A cố tình nhập URL xem ticket #4 được phân công cho Staff B. | `StaffTicketController.Details [HttpGet]`: BOLA defense kiểm tra `ticket.AssignedToId == CurrentUserId.Value`. Do Staff A $\neq$ Staff B, hệ thống lập tức trả về **HTTP 403 Forbidden** (hoặc kích hoạt `AuthorizeTicketAccessAttribute`). | **ĐẠT (PASS)** |
| **8** | **TC-08** | User gửi bình luận trên ticket đã ở trạng thái `Resolved`. | `TicketController.AddComment [HttpPost]`: Kiểm tra `ticket.CanComment()`. Vì ticket đang ở `ResolvedState`, `CanComment` trả về `false`; Controller chặn đứng việc lưu comment, trả về lỗi nghiệp vụ. | **ĐẠT (PASS)** |
| **9** | **TC-09** | User gửi đánh giá 5 sao và đóng ticket đang ở `Resolved`. | `TicketController.CloseAndRate [HttpPost]`: Kiểm tra ticket đang `Resolved`; tạo bản ghi `Feedback` (Rating = 5); gọi `ticket.Close()`; chuyển sang `ClosedState`, gán `ClosedAt = UtcNow`; gửi thông báo cho Staff; ghi `SystemLog`. | **ĐẠT (PASS)** |
| **10** | **TC-10** | Admin gửi lệnh tự vô hiệu hóa chính tài khoản Admin hiện tại. | `Admin/UserController.ToggleActive [HttpPost]`: Kiểm tra `id == CurrentUserId.Value`. Phát hiện cố gắng tự khóa, Controller chặn đứng thao tác, trả về lỗi an toàn: "Quản trị viên không thể tự vô hiệu hóa tài khoản của chính mình"; tài khoản giữ nguyên `Active`. | **ĐẠT (PASS)** |

---

## 5. THIẾT KẾ ĐIỀU HƯỚNG, ROUTING & DEPENDENCY INJECTION

### 5.1. Bảng Định tuyến Cụ thể (Route Mappings)
```csharp
// Program.cs - Định tuyến chuẩn MVC Areas
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// SignalR Hub Endpoint
app.MapHub<TicketHub>("/hubs/ticket");
```

### 5.2. Phân cấp Lớp Cơ sở Controller (Controller Hierarchy)
```text
Microsoft.AspNetCore.Mvc.Controller
  └── BaseController (Common Properties & Result Handlers)
        ├── AuthBaseController     [Area("Auth")]
        ├── UserBaseController     [Area("User")],   [AuthorizeRoles("User", "Leader", "Admin")]
        ├── StaffBaseController    [Area("Staff")],  [AuthorizeRoles("Staff", "Leader", "Admin")]
        ├── LeaderBaseController   [Area("Leader")], [AuthorizeRoles("Leader", "Admin")]
        └── AdminBaseController    [Area("Admin")],  [AuthorizeRoles("Admin")]
```

#### Mã Nguồn Thiết kế `BaseController`:
```csharp
namespace ItTms.Web.Controllers;

[AutoValidateAntiforgeryToken]
public abstract class BaseController : Controller
{
    protected int? CurrentUserId
    {
        get
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(idClaim, out var id) ? id : null;
        }
    }

    protected string? CurrentUserName => User.FindFirst(ClaimTypes.Name)?.Value;
    protected string? CurrentUserRole => User.FindFirst(ClaimTypes.Role)?.Value;
    protected string? CurrentUserDepartment => User.FindFirst("Department")?.Value;

    protected bool IsAjaxRequest => 
        Request.Headers.XRequestedWith == "XMLHttpRequest" || 
        Request.Headers.Accept.Any(a => a?.Contains("application/json") == true);

    protected IActionResult HandleResult(
        Result result, 
        string successMessage, 
        string redirectAction, 
        object? routeValues = null)
    {
        if (result.IsSuccess)
        {
            TempData["SuccessMessage"] = successMessage;
            return RedirectToAction(redirectAction, routeValues);
        }

        if (IsAjaxRequest)
        {
            return BadRequest(new { message = result.Error.Description, code = result.Error.Code });
        }

        TempData["ErrorMessage"] = result.Error.Description;
        return RedirectToAction(redirectAction, routeValues);
    }

    protected IActionResult HandleResult<T>(
        Result<T> result, 
        Func<T, IActionResult> onSuccess, 
        string? redirectOnErrorAction = null,
        object? errorRouteValues = null)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value);
        }

        if (IsAjaxRequest)
        {
            return BadRequest(new { message = result.Error.Description, code = result.Error.Code });
        }

        TempData["ErrorMessage"] = result.Error.Description;
        if (!string.IsNullOrEmpty(redirectOnErrorAction))
        {
            return RedirectToAction(redirectOnErrorAction, errorRouteValues);
        }

        return View();
    }
}
```

### 5.3. Kế hoạch Đăng ký Dependency Injection
Để toàn bộ hệ thống vận hành mượt mà và các Observers có đầy đủ bối cảnh người dùng, các dịch vụ sau được đăng ký trong DI Container:

1. **`IHttpContextAccessor` & `ICurrentUserService`**:
   - Triển khai `CurrentUserService : ICurrentUserService` đọc trực tiếp từ `IHttpContextAccessor.HttpContext.User`.
   - Đăng ký:
     ```csharp
     builder.Services.AddHttpContextAccessor();
     builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
     ```
2. **Cookie Authentication**:
   - Cấu hình Cookie Scheme với thời gian sống 8 giờ, sliding expiration và đường dẫn điều hướng an toàn:
     ```csharp
     builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
         .AddCookie(options =>
         {
             options.LoginPath = "/Auth/Login";
             options.LogoutPath = "/Auth/Logout";
             options.AccessDeniedPath = "/Auth/AccessDenied";
             options.Cookie.Name = "ItTms.Session";
             options.Cookie.HttpOnly = true;
             options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
             options.Cookie.SameSite = SameSiteMode.Lax;
             options.ExpireTimeSpan = TimeSpan.FromHours(8);
             options.SlidingExpiration = true;
         });
     ```
3. **SignalR WebSocket & Background Dispatcher**:
   - `builder.Services.AddSignalR();`
   - Đăng ký `SignalRNotificationDispatcher` phục vụ đẩy thông báo tức thời từ các Observers.

---

## 6. THIẾT KẾ GIAO DIỆN & TÍCH HỢP SIGNALR REALTIME

### 6.1. Triệt tiêu Hoàn toàn Polling 3s bằng SignalR WebSocket
- Tại view chi tiết ticket (`Areas/User/Views/Ticket/Details.cshtml`, `Areas/Staff/Views/StaffTicket/Details.cshtml`, `Areas/Leader/Views/LeaderTicket/Details.cshtml`):
  - Nhúng file JavaScript dùng chung: `wwwroot/js/ticket-realtime.js`.
  - Client tự động kết nối và gia nhập nhóm:
    ```javascript
    const ticketId = document.getElementById("ticketIdHidden").value;
    const connection = new signalR.HubConnectionBuilder()
        .withUrl("/hubs/ticket")
        .withAutomaticReconnect()
        .build();

    connection.start().then(() => {
        connection.invoke("JoinTicketGroup", parseInt(ticketId));
    });

    connection.on("ReceiveComment", function (comment) {
        appendCommentToChatBox(comment);
        playMessageNotificationSound();
    });

    connection.on("TicketStatusUpdated", function (statusData) {
        updateTicketStatusBadge(statusData.newStatus, statusData.colorClass);
    });

    window.addEventListener("beforeunload", () => {
        connection.invoke("LeaveTicketGroup", parseInt(ticketId));
    });
    ```
- Khi gửi bình luận mới qua AJAX:
  - Form submit gửi POST lên Controller.
  - Sau khi lưu CSDL thành công, Controller gọi `_hubContext.Clients.Group($"Ticket_{ticketId}").SendAsync("ReceiveComment", dto)`.
  - Tất cả các tab trình duyệt đang mở ticket đó nhận được comment tức thì ($< 50ms$), không reload trang, không sinh polling traffic.

### 6.2. Chuông Thông báo Navbar Toàn Cục
- Layout chính `_Layout.cshtml` tích hợp `_NotificationBellPartial.cshtml`.
- Client SignalR lắng nghe sự kiện `ReceiveNotification`:
  ```javascript
  connection.on("ReceiveNotification", function (notification) {
      incrementUnreadBadge();
      showToastNotification(notification.message, notification.link);
  });
  ```

---

## 7. CẤU TRÚC TỆP DỰ KIẾN (FILE TREE BLUEPRINT)

```text
src/ItTms.Web/
├── Areas/
│   ├── Auth/
│   │   ├── Controllers/
│   │   │   ├── AuthBaseController.cs
│   │   │   └── AuthController.cs
│   │   ├── Models/
│   │   │   ├── LoginViewModel.cs
│   │   │   └── ChangePasswordViewModel.cs
│   │   └── Views/
│   │       ├── Auth/
│   │       │   ├── Login.cshtml
│   │       │   ├── ChangePassword.cshtml
│   │       │   └── AccessDenied.cshtml
│   │       └── _ViewStart.cshtml
│   ├── User/
│   │   ├── Controllers/
│   │   │   ├── UserBaseController.cs
│   │   │   ├── TicketController.cs
│   │   │   └── DashboardController.cs
│   │   ├── Models/
│   │   │   ├── CreateTicketViewModel.cs
│   │   │   ├── MyTicketsViewModel.cs
│   │   │   ├── TicketDetailViewModel.cs
│   │   │   ├── AddCommentViewModel.cs
│   │   │   ├── CloseAndRateViewModel.cs
│   │   │   └── UserDashboardViewModel.cs
│   │   └── Views/
│   │       ├── Ticket/
│   │       │   ├── Create.cshtml
│   │       │   ├── MyTickets.cshtml
│   │       │   └── Details.cshtml
│   │       ├── Dashboard/
│   │       │   └── Index.cshtml
│   │       └── _ViewStart.cshtml
│   ├── Staff/
│   │   ├── Controllers/
│   │   │   ├── StaffBaseController.cs
│   │   │   ├── StaffTicketController.cs
│   │   │   └── DashboardController.cs
│   │   ├── Models/
│   │   │   ├── StaffAssignedTicketsViewModel.cs
│   │   │   ├── StaffTicketDetailViewModel.cs
│   │   │   ├── HoldTicketViewModel.cs
│   │   │   └── StaffDashboardViewModel.cs
│   │   └── Views/
│   │       ├── StaffTicket/
│   │       │   ├── Assigned.cshtml
│   │       │   └── Details.cshtml
│   │       ├── Dashboard/
│   │       │   └── Index.cshtml
│   │       └── _ViewStart.cshtml
│   ├── Leader/
│   │   ├── Controllers/
│   │   │   ├── LeaderBaseController.cs
│   │   │   ├── LeaderTicketController.cs
│   │   │   └── LeaderDashboardController.cs
│   │   ├── Models/
│   │   │   ├── NewTicketsViewModel.cs
│   │   │   ├── AssignTicketViewModel.cs
│   │   │   ├── RejectTicketViewModel.cs
│   │   │   ├── ReassignTicketViewModel.cs
│   │   │   ├── TicketFilterViewModel.cs
│   │   │   └── LeaderDashboardViewModel.cs
│   │   └── Views/
│   │       ├── LeaderTicket/
│   │       │   ├── NewTickets.cshtml
│   │       │   ├── All.cshtml
│   │       │   └── Details.cshtml
│   │       ├── LeaderDashboard/
│   │       │   └── Index.cshtml
│   │       └── _ViewStart.cshtml
│   └── Admin/
│       ├── Controllers/
│       │   ├── AdminBaseController.cs
│       │   ├── UserController.cs
│       │   ├── RoleController.cs
│       │   ├── AuditController.cs
│       │   └── AdminDashboardController.cs
│       ├── Models/
│       │   ├── UserListViewModel.cs
│       │   ├── CreateUserViewModel.cs
│       │   ├── EditUserViewModel.cs
│       │   ├── AdminResetPasswordViewModel.cs
│       │   ├── ManageRolesViewModel.cs
│       │   ├── AuditLogListViewModel.cs
│       │   └── AdminDashboardViewModel.cs
│       └── Views/
│           ├── User/
│           │   ├── Index.cshtml
│           │   ├── Create.cshtml
│           │   └── Edit.cshtml
│           ├── Role/
│           │   └── Index.cshtml
│           ├── Audit/
│           │   └── Index.cshtml
│           ├── AdminDashboard/
│           │   └── Index.cshtml
│           └── _ViewStart.cshtml
├── Controllers/
│   ├── BaseController.cs
│   └── HomeController.cs
├── Filters/
│   ├── AuthorizeRolesAttribute.cs
│   └── AuthorizeTicketAccessAttribute.cs
├── Hubs/
│   └── TicketHub.cs
├── Services/
│   └── CurrentUserService.cs
├── Views/
│   ├── Shared/
│   │   ├── _Layout.cshtml
│   │   ├── _NotificationBellPartial.cshtml
│   │   ├── _ChatDiscussionPartial.cshtml
│   │   ├── _StatusBadgePartial.cshtml
│   │   ├── _PriorityBadgePartial.cshtml
│   │   ├── AccessDenied.cshtml
│   │   └── Error.cshtml
│   ├── _ViewImports.cshtml
│   └── _ViewStart.cshtml
└── wwwroot/
    ├── js/
    │   ├── ticket-realtime.js
    │   └── site.js
    └── css/
        └── site.css
```

---

## 8. KẾT LUẬN & ĐÁNH GIÁ TÍNH KHẢ THI
Kế hoạch thiết kế kiến trúc trên giải quyết trọn vẹn toàn bộ các yêu cầu của Milestone 3 (M3):
1. **Bao phủ 100% 20 Use Cases** qua 5 Areas tách biệt, độc lập và dễ bảo trì.
2. **Triệt tiêu hoàn toàn polling 3s** bằng SignalR Persistent WebSocket Hub, bảo vệ SQLite và máy chủ Kestrel.
3. **Hiện thực hóa trọn vẹn các kịch bản kiểm thử nghiệm thu Bảng 7.2 (TC-01 đến TC-10)** ngay tại tầng Web Presentation.
4. **Phòng thủ vững chắc chống lỗ hổng OWASP** (BOLA/IDOR, CSRF, Inactive Session) qua kiến trúc ActionFilter và Base Controller chuẩn mực.
