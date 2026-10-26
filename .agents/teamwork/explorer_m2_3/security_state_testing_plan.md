# 🛡️ THIẾT KẾ KIẾN TRÚC BỘ LỌC AN NINH (SECURITY FILTERS) VÀ KẾ HOẠCH KIỂM THỬ MA TRẬN 49 HOÁN VỊ STATE MACHINE (IT-TMS .NET 9)

> **Dự án**: Hệ thống Quản lý Ticket Yêu cầu Hỗ trợ Kỹ thuật IT (IT-TMS)  
> **Tác giả**: explorer_m2_3 | Nhóm 7 - HCMUTE  
> **Ngày lập**: 2026-10-25 | Phiên bản: 1.0 (Production-Grade Architecture)  
> **Tiêu chuẩn áp dụng**: `#Argon-Omega` Cognitive Kernel | Zero-Trust OWASP API Top 10 (2023/2026) | Detroit/Classical School Testing  
> **Phạm vi tài liệu**: Đặc tả thiết kế `AuthorizeRolesAttribute`, Bộ lọc phòng thủ chống BOLA/IDOR `AuthorizeTicketAccessFilter`, và Ma trận Unit Test 49 hoán vị State Pattern trong `tests/ItTms.Tests`.

---

## MỤC LỤC
1. [TỔNG QUAN KIẾN TRÚC & NGUYÊN TẮC BẢO MẬT BẤT BIẾN](#1-tổng-quan-kiến-trúc--nguyên-tắc-bảo-mật-bất-biến)
2. [ĐẶC TẢ THIẾT KẾ BỘ LỌC PHÂN QUYỀN VAI TRÒ & KHÓA TÀI KHOẢN (AUTHORIZEROLESATTRIBUTE)](#2-đặc-tả-thiết-kế-bộ-lọc-phân-quyền-vai-trò--khóa-tài-khoản-authorizerolesattribute)
3. [ĐẶC TẢ THIẾT KẾ BỘ LỌC PHÒNG VỆ CHỐNG BOLA / IDOR (AUTHORIZETICKETACCESSFILTER)](#3-đặc-tả-thiết-kế-bộ-lọc-phòng-vệ-chống-bola--idor-authorizeticketaccessfilter)
4. [MA TRẬN KIỂM THỬ TOÀN DIỆN 49 HOÁN VỊ STATE MACHINE (7 STATES × 7 ACTIONS)](#4-ma-trận-kiểm-thử-toàn-diện-49-hoán-vị-state-machine-7-states--7-actions)
5. [ĐẶC TẢ KIỂM THỬ THUỘC TÍNH BẤT BIẾN CANCOMMENT() VÀ TRUY XUẤT NGƯỢC](#5-đặc-tả-kiểm-thử-thuộc-tính-bất-biến-cancomment-và-truy-xuất-ngược)
6. [KẾ HOẠCH TRIỂN KHAI BỘ TEST SUITE TRONG TESTS/ITTMS.TESTS](#6-kế-hoạch-triển-khai-bộ-test-suite-trong-testsittmstests)
7. [HƯỚNG DẪN BÀN GIAO CHO WORKER & AUDITOR](#7-hướng-dẫn-bàn-giao-cho-worker--auditor)

---

## 1. TỔNG QUAN KIẾN TRÚC & NGUYÊN TẮC BẢO MẬT BẤT BIẾN

### 1.1. Triết lý Thiết kế (#Argon-Omega Zero-Trust Defense-in-Depth)
Hệ thống IT-TMS quản lý các thông tin kỹ thuật, tài sản phần cứng và dữ liệu tài khoản nhạy cảm của toàn bộ doanh nghiệp. Mọi lỗ hổng phân quyền (Broken Access Control) hay truy cập đối tượng trái phép (Broken Object Level Authorization - BOLA/IDOR) đều dẫn đến rủi ro rò rỉ dữ liệu hoặc phá hoại quy trình vận hành IT.

Chiến lược thiết kế an ninh của IT-TMS áp dụng mô hình phòng thủ theo chiều sâu (Defense-in-Depth) gồm 3 lớp:
1. **Lớp 1 - MVC Filter Pipeline (`Web Layer`)**: Chặn đứng các truy cập trái phép ngay từ khi HTTP Request vừa chạm vào Kestrel/Action Filter, ngăn chặn hoàn toàn việc thực thi Controller Action, bảo vệ Kestrel thread pool khỏi lãng phí CPU và bộ nhớ.
2. **Lớp 2 - Application / Handler Pipeline (`Application Layer`)**: Xác thực quyền sở hữu và vai trò thông qua `ICurrentUserService` trước khi nạp Domain Model vào ngữ cảnh xử lý.
3. **Lớp 3 - Rich Domain Model (`Domain Layer`)**: Các phương thức nghiệp vụ của Aggregate Root `Ticket` và `User` (`StartProgress`, `PutOnHold`, `Resolve`, `Close`, `Reopen`, `Deactivate`) đóng gói sẵn các mệnh đề bảo vệ (Invariant Guards), trả về `Result.Failure(Error)` thuần khiết thay vì ném exception.

### 1.2. Ánh xạ Trực tiếp tới 10 Kịch bản Nghiệm thu Bảng 7.2
Bộ lọc bảo mật và kế hoạch kiểm thử này đóng vai trò quyết định trong việc pass 100% các kịch bản kiểm chứng quan trọng của đồ án:
- **Bảng 7.2 STT 1 (TC-01)**: Đăng nhập/truy cập bằng tài khoản đã bị vô hiệu hóa (`IsActive == false`) -> Bị từ chối, trả về lỗi 401 hoặc chuyển hướng về Login kèm thông báo *"Tài khoản đã bị vô hiệu hóa"*.
- **Bảng 7.2 STT 2 (TC-02)**: User truy cập trang của Leader (hoặc Admin/Staff) mà không có vai trò phù hợp -> Bị từ chối với mã **HTTP 403 Forbidden**.
- **Bảng 7.2 STT 5 (TC-05)**: Staff cố tình đóng ticket đang ở trạng thái `New` -> State Machine từ chối chuyển trạng thái, giữ nguyên trạng thái `New`.
- **Bảng 7.2 STT 6 (TC-06)**: Leader cố tình từ chối (Reject) ticket đang ở trạng thái `InProgress` -> Bị từ chối với mã `CANNOT_REJECT_IN_PROGRESS`.
- **Bảng 7.2 STT 7 (TC-07)**: Staff mở ticket không được phân công cho mình -> Bộ lọc Anti-BOLA/IDOR chặn đứng với mã **HTTP 403 Forbidden** (hoặc 404).
- **Bảng 7.2 STT 8 (TC-08)**: User gửi bình luận trên ticket đã `Resolved` -> Bị chặn đứng bởi `ticket.CanComment() == false`.
- **Bảng 7.2 STT 9 (TC-09)**: User xác nhận đóng và đánh giá ticket `Resolved` -> State Machine chuyển sang `Closed` kèm bản ghi `Feedback` và audit log.
- **Bảng 7.2 STT 10 (TC-10)**: Admin toggle tài khoản người khác & chặn tự vô hiệu hóa chính mình (`ADMIN_CANNOT_DEACTIVATE_SELF`).

---

## 2. ĐẶC TẢ THIẾT KẾ BỘ LỌC PHÂN QUYỀN VAI TRÒ & KHÓA TÀI KHOẢN (AUTHORIZEROLESATTRIBUTE)

### 2.1. Phân tích Nguyên lý & Nguy cơ "Stale Session"
Trong kiến trúc xác thực dựa trên Cookie (`CookieAuthenticationDefaults.AuthenticationScheme`), khi người dùng đăng nhập thành công, một Cookie chứa các Claims (UserId, Username, Role) được mã hóa và gửi về trình duyệt.

**Hiểm họa Stale Session (Tài khoản bị khóa nhưng phiên vẫn sống)**:
- Nếu Admin thực hiện vô hiệu hóa tài khoản của Staff A (`IsActive = false`), nhưng Cookie của Staff A có thời hạn (TTL) 8 giờ.
- Nếu hệ thống chỉ kiểm tra Role và Identity trong Cookie (`User.IsInRole(...)`), Staff A vẫn tiếp tục gọi API, xem ticket, sửa dữ liệu nội bộ trong suốt 8 giờ đó mà không bị ngăn chặn!
- **Giải pháp `#Argon-Omega`**: `AuthorizeRolesAttribute` bắt buộc phải là một **ActionFilter thời gian thực (Real-time Action Filter)**. Tại mỗi request đi vào controller được bảo vệ, bộ lọc sẽ trích xuất `UserId` từ Claims, truy vấn trạng thái `IsActive` trực tiếp từ CSDL (thông qua `IItTmsDbContext` với `AsNoTracking()` siêu nhẹ). Nếu tài khoản đã bị vô hiệu hóa, phiên đăng nhập bị hủy ngay lập tức (`SignOutAsync`) và request bị từ chối tức thì (TC-01).

### 2.2. Kiến trúc Lớp Decorator / Action Filter

```
                    HTTP Request
                         │
                         ▼
             ┌─────────────────────────┐
             │ [AuthorizeRoles(roles)] │
             └───────────┬─────────────┘
                         │
                         ▼
        ┌───────────────────────────────────┐
        │ 1. Endpoint có [AllowAnonymous]?  │──► CÓ ──► Cho phép đi tiếp (next())
        └────────────────┬──────────────────┘
                         │ KHÔNG
                         ▼
        ┌───────────────────────────────────┐
        │ 2. Đã xác thực Identity?          │──► KHÔNG ──► 401 Unauthorized / Redirect /Auth/Login
        └────────────────┬──────────────────┘
                         │ CÓ
                         ▼
        ┌───────────────────────────────────┐
        │ 3. Tra cứu IsActive trong CSDL    │──► INACTIVE ──► Hủy Cookie, trả 401 /
        └────────────────┬──────────────────┘                 Redirect /Auth/Login (TC-01)
                         │ ACTIVE (IsActive == true)
                         ▼
        ┌───────────────────────────────────┐
        │ 4. User Role ∈ AllowedRoles?      │──► KHÔNG ──► 403 Forbidden /
        └────────────────┬──────────────────┘              Redirect /Auth/AccessDenied (TC-02)
                         │ CÓ
                         ▼
                ┌──────────────────┐
                │ await next();    │ ──► Tiếp tục vào Controller Action
                └──────────────────┘
```

### 2.3. Mã Nguồn Thiết kế Chi tiết C# (`AuthorizeRolesAttribute.cs`)

Vị trí lưu trữ: `src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs`

```csharp
namespace ItTms.Web.Filters;

using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Enums;

/// <summary>
/// Decorator / ActionFilter Pattern: Thực thi kiểm soát truy cập dựa trên vai trò (RBAC) 
/// và kiểm tra trạng thái kích hoạt tài khoản trong thời gian thực (Bảng 7.2 TC-01 & TC-02).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthorizeRolesAttribute : Attribute, IAsyncActionFilter
{
    private readonly string[] _allowedRoles;

    public AuthorizeRolesAttribute(params string[] roles)
    {
        _allowedRoles = roles ?? Array.Empty<string>();
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetService<ILogger<AuthorizeRolesAttribute>>();

        // 1. Kiểm tra nếu Endpoint hoặc Action có gắn [AllowAnonymous] -> Bỏ qua kiểm tra an ninh
        var endpoint = httpContext.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<IAllowAnonymous>() != null ||
            context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            await next();
            return;
        }

        // 2. Kiểm tra tính xác thực của danh tính người dùng (Authentication Check)
        var user = httpContext.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            logger?.LogWarning("❌ [SECURITY] Chưa xác thực: Cố gắng truy cập tài nguyên bảo vệ tại {Path}", httpContext.Request.Path);
            HandleUnauthorized(context);
            return;
        }

        // Trích xuất UserId từ Claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
        {
            logger?.LogWarning("❌ [SECURITY] Claim UserId không hợp lệ: '{ClaimValue}'", userIdClaim);
            HandleUnauthorized(context);
            return;
        }

        // 3. Kiểm tra trạng thái kích hoạt tài khoản trong thời gian thực từ CSDL (Table 7.2 TC-01)
        var db = httpContext.RequestServices.GetRequiredService<IItTmsDbContext>();
        var userEntity = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Status, u.Role, u.Username })
            .FirstOrDefaultAsync(u => u.Id == userId, context.HttpContext.RequestAborted);

        if (userEntity == null || userEntity.Status != UserStatus.Active)
        {
            logger?.LogWarning("🚫 [SECURITY-TC01] Tài khoản đã bị vô hiệu hóa hoặc không tồn tại: UserId {UserId}, Username '{Username}'", 
                userId, userEntity?.Username ?? "Unknown");

            // Tự động thu hồi phiên đăng nhập ngay lập tức để bảo vệ hệ thống
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Bảng 7.2 TC-01: Từ chối truy cập và thông báo tài khoản đã bị vô hiệu hóa
            HandleInactiveAccount(context);
            return;
        }

        // 4. Kiểm tra quyền hạn vai trò (Role-Based Access Control - Table 7.2 TC-02)
        if (_allowedRoles.Length > 0)
        {
            var userRoleName = userEntity.Role.ToString();
            var hasRole = _allowedRoles.Any(role => string.Equals(role, userRoleName, StringComparison.OrdinalIgnoreCase)) ||
                          _allowedRoles.Any(role => user.IsInRole(role));

            if (!hasRole)
            {
                logger?.LogWarning("⛔ [SECURITY-TC02] Truy cập trái quyền: User {Username} (Role: {UserRole}) cố truy cập endpoint yêu cầu vai trò [{AllowedRoles}] tại {Path}",
                    userEntity.Username, userRoleName, string.Join(", ", _allowedRoles), httpContext.Request.Path);

                // Bảng 7.2 TC-02: Trả về HTTP 403 Forbidden
                HandleForbidden(context);
                return;
            }
        }

        // Mọi kiểm tra an ninh hợp lệ -> Chuyển tiếp vào Controller Action
        await next();
    }

    private static void HandleUnauthorized(ActionExecutingContext context)
    {
        if (IsAjaxOrApiRequest(context.HttpContext.Request))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Yêu cầu chưa được xác thực. Vui lòng đăng nhập." });
        }
        else
        {
            var returnUrl = context.HttpContext.Request.Path + context.HttpContext.Request.QueryString;
            context.Result = new RedirectToActionResult("Login", "Auth", new { area = "Auth", returnUrl });
        }
    }

    private static void HandleInactiveAccount(ActionExecutingContext context)
    {
        if (IsAjaxOrApiRequest(context.HttpContext.Request))
        {
            context.Result = new UnauthorizedObjectResult(new { message = "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên." });
        }
        else
        {
            if (context.Controller is Controller controller)
            {
                controller.TempData["ErrorMessage"] = "Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên hệ thống để được hỗ trợ.";
            }
            context.Result = new RedirectToActionResult("Login", "Auth", new { area = "Auth" });
        }
    }

    private static void HandleForbidden(ActionExecutingContext context)
    {
        if (IsAjaxOrApiRequest(context.HttpContext.Request))
        {
            context.Result = new ObjectResult(new { message = "Bạn không có quyền truy cập vào chức năng này." })
            {
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
        else
        {
            context.Result = new ViewResult
            {
                ViewName = "~/Views/Shared/AccessDenied.cshtml",
                StatusCode = StatusCodes.Status403Forbidden
            };
        }
    }

    private static bool IsAjaxOrApiRequest(HttpRequest request)
    {
        if (request.Headers.XRequestedWith == "XMLHttpRequest")
            return true;

        var acceptHeader = request.Headers.Accept.ToString();
        return acceptHeader.Contains("application/json", StringComparison.OrdinalIgnoreCase);
    }
}
```

---

## 3. ĐẶC TẢ THIẾT KẾ BỘ LỌC PHÒNG VỆ CHỐNG BOLA / IDOR (AUTHORIZETICKETACCESSFILTER)

### 3.1. Phân tích Mối đe dọa OWASP API1:2023 (Broken Object Level Authorization)
Trong mô hình MVC/API của hệ thống ticket:
- Các endpoint xem chi tiết ticket (`/Staff/Ticket/Detail/{id}` hoặc `/User/Ticket/Detail/{id}`) hoặc chỉnh sửa trạng thái (`/Staff/Ticket/UpdateStatus`) nhận định danh `id` hoặc `ticketId`.
- Nếu thiếu bộ lọc kiểm soát cấp đối tượng, kẻ tấn công hoặc nhân viên tò mò chỉ cần thay đổi số ID trên thanh địa chỉ URL (IDOR - Insecure Direct Object Reference) là có thể xem trộm nội dung ticket, tệp đính kèm bảo mật, hoặc thao tác trên ticket của người khác.

**Quy định Nghiệp vụ Ủy quyền Đối tượng (Object Authorization Rules - MECE)**:
1. **Admin**: Có toàn quyền xem và quản lý mọi ticket trong hệ thống.
2. **Leader**: Có quyền xem toàn bộ ticket trong hàng đợi để phân công, điều chuyển, và giám sát SLA.
3. **Staff**: Chỉ được phép xem hoặc thao tác trên ticket **được Leader phân công cho chính mình** (`ticket.AssignedToId == currentStaffId`). Nếu Staff cố tình truy cập ticket của Staff khác -> **Chặn đứng ngay với mã HTTP 403 Forbidden** (Bảng 7.2 TC-07).
4. **User**: Chỉ được phép xem và thao tác trên ticket **do chính mình tạo ra** (`ticket.CreatorId == currentUserId`). Nếu User cố tình truy cập ticket của User khác -> **Chặn đứng với mã HTTP 403 Forbidden**.

### 3.2. Mã Nguồn Thiết kế Chi tiết C# (`AuthorizeTicketAccessAttribute.cs`)

Vị trí lưu trữ: `src/ItTms.Web/Filters/AuthorizeTicketAccessAttribute.cs`

```csharp
namespace ItTms.Web.Filters;

using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Enums;

/// <summary>
/// Bộ lọc an ninh phòng thủ chống OWASP API1:2023 BOLA / IDOR trên thực thể Ticket (Bảng 7.2 TC-07).
/// Đảm bảo Staff chỉ truy cập ticket của mình, User chỉ truy cập ticket do mình tạo; 
/// Leader và Admin có thẩm quyền giám sát toàn cục.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class AuthorizeTicketAccessAttribute : Attribute, IAsyncActionFilter
{
    private readonly string _ticketIdParameterName;

    public AuthorizeTicketAccessAttribute(string ticketIdParameterName = "id")
    {
        _ticketIdParameterName = ticketIdParameterName;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var logger = httpContext.RequestServices.GetService<ILogger<AuthorizeTicketAccessAttribute>>();

        // 1. Kiểm tra xác thực cơ bản
        var user = httpContext.User;
        if (user?.Identity == null || !user.Identity.IsAuthenticated)
        {
            context.Result = new ChallengeResult();
            return;
        }

        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int currentUserId))
        {
            context.Result = new ForbidResult();
            return;
        }

        // 2. Trích xuất TicketId từ Route Data, Action Arguments hoặc Query String
        int ticketId = ExtractTicketId(context);
        if (ticketId <= 0)
        {
            logger?.LogWarning("⚠️ [BOLA-GUARD] Không thể trích xuất TicketId hợp lệ từ request tại {Path}", httpContext.Request.Path);
            context.Result = new BadRequestObjectResult(new { message = "Định danh TicketId không hợp lệ." });
            return;
        }

        // 3. Tra cứu thông tin sở hữu của Ticket trong CSDL
        var db = httpContext.RequestServices.GetRequiredService<IItTmsDbContext>();
        var ticket = await db.Tickets.AsNoTracking()
            .Select(t => new { t.Id, t.CreatorId, t.AssignedToId, t.StatusId })
            .FirstOrDefaultAsync(t => t.Id == ticketId, context.HttpContext.RequestAborted);

        if (ticket == null)
        {
            logger?.LogInformation("ℹ️ [BOLA-GUARD] Không tìm thấy Ticket #{TicketId}", ticketId);
            context.Result = new NotFoundObjectResult(new { message = $"Không tìm thấy yêu cầu #{ticketId}." });
            return;
        }

        // 4. Trích xuất vai trò hiện tại của người dùng
        var roleClaim = user.FindFirst(ClaimTypes.Role)?.Value;

        // Admin & Leader có toàn quyền giám sát hệ thống
        if (string.Equals(roleClaim, RoleNames.Admin, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(roleClaim, RoleNames.Leader, StringComparison.OrdinalIgnoreCase))
        {
            await next();
            return;
        }

        // 5. Kiểm tra phân quyền đối với vai trò Staff (Bảng 7.2 TC-07)
        if (string.Equals(roleClaim, RoleNames.Staff, StringComparison.OrdinalIgnoreCase))
        {
            if (ticket.AssignedToId != currentUserId)
            {
                logger?.LogWarning("🚫 [BOLA-GUARD TC-07] BOLA VIOLATION: Staff #{StaffId} cố tình truy cập Ticket #{TicketId} (thuộc về Staff #{AssignedStaffId})",
                    currentUserId, ticketId, ticket.AssignedToId);

                // Ghi vết cảnh báo an ninh vào bảng SystemLogs
                await LogSecurityViolationAsync(db, currentUserId, 
                    $"Staff #{currentUserId} vi phạm BOLA khi cố truy cập trái phép Ticket #{ticketId}");

                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml",
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }
        // 6. Kiểm tra phân quyền đối với vai trò User
        else if (string.Equals(roleClaim, RoleNames.User, StringComparison.OrdinalIgnoreCase))
        {
            if (ticket.CreatorId != currentUserId)
            {
                logger?.LogWarning("🚫 [BOLA-GUARD] BOLA VIOLATION: User #{UserId} cố tình truy cập Ticket #{TicketId} (thuộc về User #{CreatorId})",
                    currentUserId, ticketId, ticket.CreatorId);

                await LogSecurityViolationAsync(db, currentUserId,
                    $"User #{currentUserId} vi phạm BOLA khi cố truy cập trái phép Ticket #{ticketId}");

                context.Result = new ViewResult
                {
                    ViewName = "~/Views/Shared/AccessDenied.cshtml",
                    StatusCode = StatusCodes.Status403Forbidden
                };
                return;
            }
        }
        else
        {
            // Vai trò không xác định -> Chặn lập tức
            context.Result = new ForbidResult();
            return;
        }

        // Hợp lệ về quyền sở hữu -> Tiếp tục thực thi action
        await next();
    }

    private int ExtractTicketId(ActionExecutingContext context)
    {
        // Thử lấy từ RouteData (ví dụ: /Staff/Ticket/Detail/101 hoặc /Staff/Ticket/Detail?id=101)
        if (context.RouteData.Values.TryGetValue(_ticketIdParameterName, out var routeVal) &&
            int.TryParse(routeVal?.ToString(), out int rId))
        {
            return rId;
        }

        // Thử lấy từ fallback key "ticketId"
        if (context.RouteData.Values.TryGetValue("ticketId", out var tRouteVal) &&
            int.TryParse(tRouteVal?.ToString(), out int tId))
        {
            return tId;
        }

        // Thử lấy từ ActionArguments
        if (context.ActionArguments.TryGetValue(_ticketIdParameterName, out var argVal) &&
            int.TryParse(argVal?.ToString(), out int aId))
        {
            return aId;
        }

        if (context.ActionArguments.TryGetValue("ticketId", out var tArgVal) &&
            int.TryParse(tArgVal?.ToString(), out int atId))
        {
            return atId;
        }

        // Thử lấy từ Query String
        if (context.HttpContext.Request.Query.TryGetValue(_ticketIdParameterName, out var queryVal) &&
            int.TryParse(queryVal, out int qId))
        {
            return qId;
        }

        return 0;
    }

    private static async Task LogSecurityViolationAsync(IItTmsDbContext db, int userId, string details)
    {
        try
        {
            var log = ItTms.Domain.Entities.SystemLog.Create(
                action: "BOLA_Violation_Attempt",
                details: details,
                userId: userId,
                ipAddress: "127.0.0.1",
                createdAt: DateTime.UtcNow
            );
            db.SystemLogs.Add(log);
            await db.SaveChangesAsync();
        }
        catch
        {
            // Bảo đảm không làm gãy pipeline nếu ghi log gặp sự cố tạm thời
        }
    }
}
```

---

## 4. MA TRẬN KIỂM THỬ TOÀN DIỆN 49 HOÁN VỊ STATE MACHINE (7 STATES × 7 ACTIONS)

### 4.1. Không gian Trạng thái & 7 Hành vi Cốt lõi
Vòng đời Ticket gồm 7 trạng thái chuẩn hóa:
1. `S1 - New` (Id = 1): Mới tạo, chờ Leader phân công hoặc từ chối.
2. `S2 - Assigned` (Id = 2): Đã phân công cho Kỹ thuật viên, chờ Kỹ thuật viên tiếp nhận xử lý.
3. `S3 - InProgress` (Id = 3): Kỹ thuật viên đang trong quá trình thao tác sửa chữa.
4. `S4 - Waiting` (Id = 4): Tạm hoãn chờ người dùng bổ sung thông tin hoặc chờ thiết bị.
5. `S5 - Resolved` (Id = 5): Kỹ thuật viên đã xử lý xong sự cố, chờ người dùng nghiệm thu.
6. `S6 - Closed` (Id = 6): Người dùng đã xác nhận đóng và đánh giá, kết thúc vòng đời.
7. `S7 - Rejected` (Id = 7): Leader từ chối yêu cầu không hợp lệ ngay từ hàng đợi `New`.

7 Hành vi chuyển đổi trạng thái (Domain Transition Actions):
- `A1: Assign(staffId, leaderId)`: Phân công hoặc điều chuyển kỹ thuật viên.
- `A2: StartProgress(staffId)`: Tiếp nhận xử lý kỹ thuật hoặc phục hồi từ tạm hoãn.
- `A3: PutOnHold(staffId)`: Tạm hoãn xử lý chờ phản hồi.
- `A4: Resolve(staffId)`: Hoàn tất công việc kỹ thuật.
- `A5: Close(userId)`: Xác nhận đóng hoàn tất vòng đời ticket.
- `A6: Reopen(userId)`: Yêu cầu mở lại xử lý khi sự cố chưa triệt để.
- `A7: Reject(reason, leaderId)`: Từ chối tiếp nhận yêu cầu.

### 4.2. Bảng Ma trận 49 Ô Hoán vị Chuẩn Hóa

Mỗi ô trong ma trận được gán nhãn:
- **`[VALID]`**: `Result.IsSuccess == true`, trạng thái chuyển đổi sang trạng thái mới, phát sinh Domain Events.
- **`[INVALID]`**: `Result.IsFailure == true`, giữ nguyên trạng thái cũ (Invariant Preserved), mã lỗi tường minh, không có Domain Events chuyển trạng thái.

```
+================================================================================================================================================================+
|                                                  MA TRẬN KIỂM CHỨNG 49 HOÁN VỊ TRẠNG THÁI TICKET (7 × 7 MATRIX)                                                |
+================+==================+==================+==================+==================+==================+==================+=========================+
| Trạng thái \   | A1: Assign       | A2: StartProgress| A3: PutOnHold    | A4: Resolve      | A5: Close        | A6: Reopen       | A7: Reject              |
| Hành động      | (Phân công)      | (Bắt đầu xử lý)  | (Tạm hoãn)       | (Hoàn tất)       | (Đóng ticket)    | (Mở lại)         | (Từ chối)               |
+================+==================+==================+==================+==================+==================+==================+=========================+
| S1. New        | [VALID]          | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID] (TC-05)| [INVALID]        | [VALID]                 |
|                | -> Assigned      | Chưa phân công   | Chưa xử lý       | Chưa xử lý       | Đóng ngang từ New| Chưa đóng        | -> Rejected             |
+----------------+------------------+------------------+------------------+------------------+------------------+------------------+-------------------------+
| S2. Assigned   | [VALID]          | [VALID]          | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]               |
|                | -> Assigned (Re) | -> InProgress    | Chưa bắt đầu     | Chưa xử lý       | Chưa xử lý       | Chưa đóng        | Đã tiếp nhận            |
+----------------+------------------+------------------+------------------+------------------+------------------+------------------+-------------------------+
| S3. InProgress | [VALID]          | [INVALID]        | [VALID]          | [VALID]          | [INVALID]        | [INVALID]        | [INVALID] (TC-06)       |
|                | -> Assigned (Re) | Đang chạy rồi    | -> Waiting       | -> Resolved      | Chưa giải quyết  | Đang chạy        | Không từ chối khi đang lm|
+----------------+------------------+------------------+------------------+------------------+------------------+------------------+-------------------------+
| S4. Waiting    | [VALID]          | [VALID]          | [INVALID]        | [VALID]          | [INVALID]        | [INVALID]        | [INVALID]               |
|                | -> Assigned (Re) | -> InProgress    | Đang tạm hoãn    | -> Resolved      | Chưa giải quyết  | Chưa đóng        | Không từ chối khi đang lm|
+----------------+------------------+------------------+------------------+------------------+------------------+------------------+-------------------------+
| S5. Resolved   | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [VALID] (TC-09)  | [VALID]          | [INVALID]               |
|                | Đã giải quyết    | Cần reopen trước | Đã giải quyết    | Đã giải quyết rồi| -> Closed        | -> InProgress    | Không từ chối khi đã xong|
+----------------+------------------+------------------+------------------+------------------+------------------+------------------+-------------------------+
| S6. Closed     | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]               |
| (Terminal)     | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa          |
+----------------+------------------+------------------+------------------+------------------+------------------+------------------+-------------------------+
| S7. Rejected   | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]        | [INVALID]               |
| (Terminal)     | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa   | Vĩnh viễn khóa          |
+================+==================+==================+==================+==================+==================+==================+=========================+
```

### 4.3. Thống kê Toán học Phân bổ Ma trận
- **Tổng số trường hợp kiểm thử**: $7 \times 7 = 49$ tổ hợp.
- **Số chuyển trạng thái HỢP LỆ (Valid Transitions)**: **12 trường hợp** (24.49%)
  1. `(New, Assign) -> Assigned`
  2. `(New, Reject) -> Rejected`
  3. `(Assigned, Assign/Reassign) -> Assigned`
  4. `(Assigned, StartProgress) -> InProgress`
  5. `(InProgress, Assign/Reassign) -> Assigned`
  6. `(InProgress, PutOnHold) -> Waiting`
  7. `(InProgress, Resolve) -> Resolved`
  8. `(Waiting, Assign/Reassign) -> Assigned`
  9. `(Waiting, StartProgress) -> InProgress`
  10. `(Waiting, Resolve) -> Resolved`
  11. `(Resolved, Close) -> Closed`
  12. `(Resolved, Reopen) -> InProgress`
- **Số chuyển trạng thái BẤT HỢP LỆ (Forbidden Transitions)**: **37 trường hợp** (75.51%)
  - Trong đó có 14 trường hợp rơi vào 2 trạng thái thiết bị đóng vĩnh viễn (`Closed` và `Rejected` - Terminal States không thể bị đột biến trạng thái).
  - 23 trường hợp vi phạm quy tắc vòng đời tuần tự (đóng trước khi làm, tạm dừng khi chưa bắt đầu, từ chối khi đang làm dở...).

---

## 5. ĐẶC TẢ KIỂM THỬ THUỘC TÍNH BẤT BIẾN CANCOMMENT() VÀ TRUY XUẤT NGƯỢC

### 5.1. Bảng Kiểm chứng Thuộc tính `CanComment()` (Bảng 7.2 TC-08)
Quy tắc bất biến: Bình luận chỉ được phép trao đổi trong quá trình tiếp nhận, làm rõ và xử lý kỹ thuật. Khi ticket đã bước vào các trạng thái hoàn tất (`Resolved`, `Closed`, `Rejected`), quyền gửi bình luận phải bị khóa triệt để để ép luồng nghiệp vụ kết thúc hoặc mở lại chính thức.

```
+========================================================================================================+
|                                MA TRẬN KIỂM CHỨNG BẤT BIẾN CANCOMMENT()                                |
+----+-------------------+-----------------+-------------------------------------------------------------+
| STT| Trạng thái Ticket | `CanComment()`  | Rationale Nghiệp vụ & Đối chiếu Báo cáo                     |
+----+-------------------+-----------------+-------------------------------------------------------------+
| 01 | New               | TRUE            | User bổ sung thông tin; Leader trao đổi sơ bộ.              |
| 02 | Assigned          | TRUE            | Kỹ thuật viên trao đổi hẹn giờ, chuẩn bị thiết bị.          |
| 03 | InProgress        | TRUE            | Kỹ thuật viên yêu cầu cung cấp tài khoản, xác nhận sự cố.   |
| 04 | Waiting           | TRUE            | User phản hồi tài liệu hoặc xác nhận thiết bị sẵn sàng.     |
| 05 | Resolved          | FALSE (TC-08)   | KHÓA BÌNH LUẬN! Bắt buộc User chọn Nghiệm thu Đóng/Reopen.  |
| 06 | Closed            | FALSE           | KHÓA VĨNH VIỄN! Hồ sơ đã lưu trữ, nghiêm cấm chỉnh sửa.     |
| 07 | Rejected          | FALSE           | KHÓA! Yêu cầu đã bị hủy, không tiếp tục thảo luận.          |
+----+-------------------+-----------------+-------------------------------------------------------------+
```

### 5.2. Ràng buộc Ủy quyền Tác tử (Actor Authorization Invariants)
Ngoài trạng thái, mỗi hành vi còn yêu cầu đúng đối tượng tác tử:
- `StartProgress`: Chỉ Staff được phân công (`AssignedToId == currentStaffId`) mới được quyền bấm bắt đầu; Staff khác bị từ chối với lỗi `UNAUTHORIZED_STAFF`.
- `PutOnHold`: Chỉ Staff được phân công mới được tạm hoãn; Staff khác bị lỗi `UNAUTHORIZED_STAFF`.
- `Resolve`: Chỉ Staff được phân công mới được báo hoàn thành; Staff khác bị lỗi `UNAUTHORIZED_STAFF`.
- `Close`: Chỉ User tạo ticket (`CreatorId == currentUserId`) mới có quyền nghiệm thu và đóng; User khác bị lỗi `UNAUTHORIZED_USER`.
- `Reopen`: Chỉ User tạo ticket mới có quyền yêu cầu mở lại; User khác bị lỗi `UNAUTHORIZED_USER`.
- `Reject`: Chỉ Leader mới có quyền từ chối yêu cầu; và chỉ được từ chối khi ticket đang ở `New` (TC-06).

---

## 6. KẾ HOẠCH TRIỂN KHAI BỘ TEST SUITE TRONG TESTS/ITTMS.TESTS

### 6.1. Cấu trúc Tệp Kiểm thử Dự kiến
Để bảo đảm tính tổ chức cao theo Clean Architecture và tiêu chuẩn kiểm thử Detroit School:
```
tests/ItTms.Tests/
├── UnitTests/
│   ├── Domain/
│   │   ├── StateMachine49PermutationTests.cs     # [MỚI] 49 test cases bao phủ 100% ma trận
│   │   ├── TicketCanCommentInvariantTests.cs     # [MỚI] Kiểm chứng CanComment() & Actor Guards
│   │   ├── TicketInvariantTests.cs               # Đã có từ M1 (SLA & Lifecycle)
│   │   └── UserInvariantTests.cs                 # Đã có từ M1 (TC-10 Self-Deactivation)
│   └── Security/
│       ├── AuthorizeRolesAttributeTests.cs       # [MỚI] Unit test RBAC & Inactive account (TC-01, TC-02)
│       └── AuthorizeTicketAccessFilterTests.cs   # [MỚI] Unit test BOLA/IDOR protection (TC-07)
```

### 6.2. Mã Nguồn Kiểm thử Ma trận 49 Hoán vị C# (`StateMachine49PermutationTests.cs`)

Vị trí lưu trữ: `tests/ItTms.Tests/UnitTests/Domain/StateMachine49PermutationTests.cs`

```csharp
namespace ItTms.Tests.UnitTests.Domain;

using System;
using System.Collections.Generic;
using FluentAssertions;
using ItTms.Domain.Common;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using Xunit;

public class StateMachine49PermutationTests
{
    private const int CreatorId = 10;
    private const int AssignedStaffId = 5;
    private const int LeaderId = 2;

    public enum TicketAction
    {
        Assign,
        StartProgress,
        PutOnHold,
        Resolve,
        Close,
        Reopen,
        Reject
    }

    public static IEnumerable<object[]> Get49PermutationsData()
    {
        int[] statuses = [
            TicketStatus.New,
            TicketStatus.Assigned,
            TicketStatus.InProgress,
            TicketStatus.Waiting,
            TicketStatus.Resolved,
            TicketStatus.Closed,
            TicketStatus.Rejected
        ];

        TicketAction[] actions = [
            TicketAction.Assign,
            TicketAction.StartProgress,
            TicketAction.PutOnHold,
            TicketAction.Resolve,
            TicketAction.Close,
            TicketAction.Reopen,
            TicketAction.Reject
        ];

        foreach (var status in statuses)
        {
            foreach (var action in actions)
            {
                bool isExpectedSuccess = IsTransitionAllowed(status, action);
                yield return new object[] { status, action, isExpectedSuccess };
            }
        }
    }

    private static bool IsTransitionAllowed(int currentStatus, TicketAction action) => (currentStatus, action) switch
    {
        // 1. New State
        (TicketStatus.New, TicketAction.Assign) => true,
        (TicketStatus.New, TicketAction.Reject) => true,

        // 2. Assigned State
        (TicketStatus.Assigned, TicketAction.Assign) => true, // Reassign
        (TicketStatus.Assigned, TicketAction.StartProgress) => true,

        // 3. InProgress State
        (TicketStatus.InProgress, TicketAction.Assign) => true, // Reassign
        (TicketStatus.InProgress, TicketAction.PutOnHold) => true,
        (TicketStatus.InProgress, TicketAction.Resolve) => true,

        // 4. Waiting State
        (TicketStatus.Waiting, TicketAction.Assign) => true, // Reassign
        (TicketStatus.Waiting, TicketAction.StartProgress) => true,
        (TicketStatus.Waiting, TicketAction.Resolve) => true,

        // 5. Resolved State
        (TicketStatus.Resolved, TicketAction.Close) => true,
        (TicketStatus.Resolved, TicketAction.Reopen) => true,

        // 6. Closed State & 7. Rejected State: Mọi thao tác đều bị cấm
        _ => false
    };

    [Theory]
    [MemberData(nameof(Get49PermutationsData))]
    public void StateMachine_49Permutations_StrictlyEnforcesAllowedVsForbiddenTransitions(
        int initialStatus, TicketAction action, bool expectedSuccess)
    {
        // Arrange
        var ticket = CreateTicketInStatus(initialStatus);
        ticket.StatusId.Should().Be(initialStatus);

        // Act
        Result result = ExecuteAction(ticket, action);

        // Assert
        if (expectedSuccess)
        {
            result.IsSuccess.Should().BeTrue($"Thao tác {action} từ trạng thái {initialStatus} phải HỢP LỆ");
            ticket.StatusId.Should().NotBe(initialStatus, "Trạng thái phải được chuyển dịch sau khi thực hiện thao tác hợp lệ");
        }
        else
        {
            result.IsFailure.Should().BeTrue($"Thao tác {action} từ trạng thái {initialStatus} phải BỊ TỪ CHỐI");
            ticket.StatusId.Should().Be(initialStatus, "Trạng thái ban đầu BẮT BUỘC giữ nguyên 100% khi thao tác thất bại");
        }
    }

    private static Ticket CreateTicketInStatus(int statusId)
    {
        var ticket = Ticket.Create("Test Title", "Test Description", TicketPriority.Medium, TicketCategory.Software, CreatorId).Value;

        switch (statusId)
        {
            case TicketStatus.New:
                break;
            case TicketStatus.Assigned:
                ticket.Assign(AssignedStaffId, LeaderId);
                break;
            case TicketStatus.InProgress:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                break;
            case TicketStatus.Waiting:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                ticket.PutOnHold(AssignedStaffId);
                break;
            case TicketStatus.Resolved:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                ticket.Resolve(AssignedStaffId);
                break;
            case TicketStatus.Closed:
                ticket.Assign(AssignedStaffId, LeaderId);
                ticket.StartProgress(AssignedStaffId);
                ticket.Resolve(AssignedStaffId);
                ticket.Close(CreatorId);
                break;
            case TicketStatus.Rejected:
                ticket.Reject("Lý do từ chối", LeaderId);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(statusId));
        }

        ticket.ClearDomainEvents();
        return ticket;
    }

    private static Result ExecuteAction(Ticket ticket, TicketAction action) => action switch
    {
        TicketAction.Assign => ticket.Assign(staffId: 99, leaderId: LeaderId),
        TicketAction.StartProgress => ticket.StartProgress(staffId: AssignedStaffId),
        TicketAction.PutOnHold => ticket.PutOnHold(staffId: AssignedStaffId),
        TicketAction.Resolve => ticket.Resolve(staffId: AssignedStaffId),
        TicketAction.Close => ticket.Close(userId: CreatorId),
        TicketAction.Reopen => ticket.Reopen(userId: CreatorId),
        TicketAction.Reject => ticket.Reject(reason: "Lý do từ chối thử nghiệm", leaderId: LeaderId),
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };
}
```

### 6.3. Mã Nguồn Kiểm thử Bộ lọc An ninh C# (`AuthorizeRolesAttributeTests.cs`)

Vị trí lưu trữ: `tests/ItTms.Tests/UnitTests/Security/AuthorizeRolesAttributeTests.cs`

```csharp
namespace ItTms.Tests.UnitTests.Security;

using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Infrastructure.Persistence;
using ItTms.Web.Filters;
using Xunit;

public class AuthorizeRolesAttributeTests
{
    private readonly DbContextOptions<ItTmsDbContext> _dbOptions;

    public AuthorizeRolesAttributeTests()
    {
        _dbOptions = new DbContextOptionsBuilder<ItTmsDbContext>()
            .UseSqlite($"Data Source=test_auth_{Guid.NewGuid():N}.db;Cache=Shared")
            .Options;

        using var ctx = new ItTmsDbContext(_dbOptions);
        ctx.Database.EnsureCreated();
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserIsAnonymous_RedirectsToLogin()
    {
        // Arrange
        var filter = new AuthorizeRolesAttribute(RoleNames.Leader);
        var context = CreateActionExecutingContext(isAuthenticated: false);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));

        // Assert
        context.Result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserIsInactive_RejectsAccess_Table72TC01()
    {
        // Arrange: Tạo user bị vô hiệu hóa trong CSDL (TC-01)
        int inactiveUserId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("staff_locked", "locked@local.test", "hash", "Staff Locked", UserRole.Staff, status: UserStatus.Inactive).Value;
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            inactiveUserId = user.Id;
        }

        var filter = new AuthorizeRolesAttribute(RoleNames.Staff);
        var context = CreateActionExecutingContext(isAuthenticated: true, userId: inactiveUserId, role: RoleNames.Staff);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));

        // Assert (Bảng 7.2 TC-01: Chuyển hướng kèm TempData báo tài khoản bị khóa)
        context.Result.Should().BeOfType<RedirectToActionResult>()
            .Which.ActionName.Should().Be("Login");
        
        var controller = context.Controller as Controller;
        controller?.TempData["ErrorMessage"].Should().Be("Tài khoản đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên hệ thống để được hỗ trợ.");
    }

    [Fact]
    public async Task OnActionExecutionAsync_WhenUserHasWrongRole_Returns403Forbidden_Table72TC02()
    {
        // Arrange: User thường truy cập trang của Leader (TC-02)
        int activeUserId;
        using (var ctx = new ItTmsDbContext(_dbOptions))
        {
            var user = User.Create("user_normal", "user@local.test", "hash", "Normal User", UserRole.User, status: UserStatus.Active).Value;
            ctx.Users.Add(user);
            await ctx.SaveChangesAsync();
            activeUserId = user.Id;
        }

        var filter = new AuthorizeRolesAttribute(RoleNames.Leader);
        var context = CreateActionExecutingContext(isAuthenticated: true, userId: activeUserId, role: RoleNames.User);

        // Act
        await filter.OnActionExecutionAsync(context, () => Task.FromResult<ActionExecutedContext>(null!));

        // Assert (Bảng 7.2 TC-02: Trả về HTTP 403 Forbidden)
        var viewResult = context.Result.Should().BeOfType<ViewResult>().Subject;
        viewResult.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    private ActionExecutingContext CreateActionExecutingContext(bool isAuthenticated, int userId = 1, string role = "User")
    {
        var httpContext = new DefaultHttpContext();
        var services = new ServiceCollection();
        
        services.AddScoped<IItTmsDbContext>(_ => new ItTmsDbContext(_dbOptions));
        services.AddLogging();
        services.AddAuthentication();
        httpContext.RequestServices = services.BuildServiceProvider();

        if (isAuthenticated)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Name, "testuser")
            };
            httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        }

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var mockController = new Mock<Controller>();
        mockController.Object.TempData = new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(httpContext, Mock.Of<Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider>());

        return new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            mockController.Object
        );
    }
}
```

---

## 7. HƯỚNG DẪN BÀN GIAO CHO WORKER & AUDITOR

### 7.1. Danh mục Tệp Mã Nguồn Worker Cần Triển khai
1. **`src/ItTms.Web/Filters/AuthorizeRolesAttribute.cs`**:
   - Triển khai `IAsyncActionFilter`.
   - Kết nối `IItTmsDbContext` kiểm tra `User.Status == UserStatus.Active`.
   - Phản hồi HTTP 401 / Redirect Login (TC-01) và HTTP 403 Forbidden (TC-02).
2. **`src/ItTms.Web/Filters/AuthorizeTicketAccessAttribute.cs`**:
   - Trích xuất `ticketId` từ route/query/argument.
   - Kiểm tra `Staff` (`ticket.AssignedToId == currentStaffId`) -> Chặn Staff khác với 403 (TC-07).
   - Kiểm tra `User` (`ticket.CreatorId == currentUserId`) -> Chặn User khác với 403.
   - Ghi nhật ký vi phạm an ninh `BOLA_Violation_Attempt` vào `SystemLogs`.
3. **`tests/ItTms.Tests/UnitTests/Domain/StateMachine49PermutationTests.cs`**:
   - 49 bài kiểm thử tuần tự theo ma trận $7 \times 7$.
   - Khẳng định tính bảo toàn trạng thái trên toàn bộ các nhánh cấm (`result.IsFailure == true`, giữ nguyên `StatusId`).
4. **`tests/ItTms.Tests/UnitTests/Security/AuthorizeRolesAttributeTests.cs`**:
   - Kiểm thử tự động TC-01 (Tài khoản bị khóa) và TC-02 (User truy cập trang Leader).
5. **`tests/ItTms.Tests/UnitTests/Security/AuthorizeTicketAccessFilterTests.cs`**:
   - Kiểm thử tự động TC-07 (Staff truy cập ticket người khác).

### 7.2. Tiêu chí Nghiệm thu Tự động (Definition of Done - DoD)
- Toàn bộ solution biên dịch sạch với 0 warning, 0 error (`dotnet build`).
- Bộ unit test State Machine 49 hoán vị chạy vượt qua 100% (`49 passed / 49 total`).
- Các test cases kiểm thử an ninh (TC-01, TC-02, TC-07) pass 100%.
- Không sử dụng `throw new Exception` trong các luồng vi phạm nghiệp vụ; 100% tuân thủ Railway-oriented `Result<T>`.
