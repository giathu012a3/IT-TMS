using System.Security.Claims;

namespace ItTms.Controllers;

/// <summary>
/// Controller cơ sở cung cấp danh tính người dùng và các tiện ích xử lý kết quả đường sắt (Result).
/// </summary>
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
}
