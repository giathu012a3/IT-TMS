using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ItTms.Domain.Common;

namespace ItTms.Web.Controllers;

/// <summary>
/// Controller cơ sở cung cấp các thuộc tính danh tính và hàm xử lý Result đường sắt (Railway-Oriented) dùng chung.
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
