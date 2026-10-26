using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;

namespace ItTms.Web.Filters;

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
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

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
        if (!int.TryParse(userIdClaim, out int currentUserId) || currentUserId <= 0)
        {
            context.Result = new ForbidResult();
            return;
        }

        // 2. Trích xuất TicketId từ Route Data, Action Arguments hoặc Query String
        int ticketId = ExtractTicketId(context);
        if (ticketId <= 0)
        {
            logger?.LogWarning("Không thể trích xuất TicketId hợp lệ từ request tại {Path}", httpContext.Request.Path);
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
            logger?.LogInformation("Không tìm thấy Ticket #{TicketId}", ticketId);
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
                logger?.LogWarning("BOLA VIOLATION: Staff #{StaffId} cố tình truy cập Ticket #{TicketId} (thuộc về Staff #{AssignedStaffId})",
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
                logger?.LogWarning("BOLA VIOLATION: User #{UserId} cố tình truy cập Ticket #{TicketId} (thuộc về User #{CreatorId})",
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

        // Thử lấy từ fallback key "id"
        if (context.RouteData.Values.TryGetValue("id", out var idRouteVal) &&
            int.TryParse(idRouteVal?.ToString(), out int iId))
        {
            return iId;
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

        if (context.ActionArguments.TryGetValue("id", out var idArgVal) &&
            int.TryParse(idArgVal?.ToString(), out int aiId))
        {
            return aiId;
        }

        // Thử lấy từ Query String
        if (context.HttpContext.Request.Query.TryGetValue(_ticketIdParameterName, out var queryVal) &&
            int.TryParse(queryVal, out int qId))
        {
            return qId;
        }

        if (context.HttpContext.Request.Query.TryGetValue("ticketId", out var tQueryVal) &&
            int.TryParse(tQueryVal, out int tqId))
        {
            return tqId;
        }

        if (context.HttpContext.Request.Query.TryGetValue("id", out var idQueryVal) &&
            int.TryParse(idQueryVal, out int iqId))
        {
            return iqId;
        }

        return 0;
    }

    private static async Task LogSecurityViolationAsync(IItTmsDbContext db, int userId, string details)
    {
        try
        {
            var log = SystemLog.Create(
                action: "BOLA_Violation_Attempt",
                details: details,
                userId: userId,
                ipAddress: "127.0.0.1"
            );
            if (log.IsSuccess)
            {
                await db.SystemLogs.AddAsync(log.Value);
                await db.SaveChangesAsync();
            }
        }
        catch
        {
            // Bảo đảm không làm gãy pipeline nếu ghi log gặp sự cố tạm thời
        }
    }
}
