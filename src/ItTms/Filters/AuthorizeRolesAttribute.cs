using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;



namespace ItTms.Filters;

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
        _allowedRoles = roles ?? [];
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

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
            logger?.LogWarning("Chưa xác thực: Cố gắng truy cập tài nguyên bảo vệ tại {Path}", httpContext.Request.Path);
            HandleUnauthorized(context);
            return;
        }

        // Trích xuất UserId từ Claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
        {
            logger?.LogWarning("Claim UserId không hợp lệ: '{ClaimValue}'", userIdClaim);
            HandleUnauthorized(context);
            return;
        }

        // 3. Kiểm tra trạng thái kích hoạt tài khoản trong thời gian thực từ CSDL (Bảng 7.2 TC-01)
        var db = httpContext.RequestServices.GetRequiredService<IItTmsDbContext>();
        var userEntity = await db.Users.AsNoTracking()
            .Select(u => new { u.Id, u.Status, u.Role, u.Username })
            .FirstOrDefaultAsync(u => u.Id == userId, context.HttpContext.RequestAborted);

        if (userEntity == null || userEntity.Status != UserStatus.Active)
        {
            logger?.LogWarning("Tài khoản đã bị vô hiệu hóa hoặc không tồn tại: UserId {UserId}, Username '{Username}'", 
                userId, userEntity?.Username ?? "Unknown");

            // Tự động thu hồi phiên đăng nhập ngay lập tức để bảo vệ hệ thống
            try
            {
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
            catch
            {
                // SignOut có thể không có cookie scheme trong ngữ cảnh test đơn lẻ
            }

            // Bảng 7.2 TC-01: Từ chối truy cập và thông báo tài khoản đã bị vô hiệu hóa
            HandleInactiveAccount(context);
            return;
        }

        // 4. Kiểm tra quyền hạn vai trò (Role-Based Access Control - Bảng 7.2 TC-02)
        if (_allowedRoles.Length > 0)
        {
            var userRoleName = userEntity.Role.ToString();
            var hasRole = _allowedRoles.Any(role => string.Equals(role, userRoleName, StringComparison.OrdinalIgnoreCase));

            if (!hasRole)
            {
                logger?.LogWarning("Truy cập trái quyền: User {Username} (Role: {UserRole}) cố truy cập endpoint yêu cầu vai trò [{AllowedRoles}] tại {Path}",
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
