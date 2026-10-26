using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace ItTms.Controllers;

public class AuthController : BaseController
{
    private readonly ItTmsDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        ItTmsDbContext dbContext,
        IPasswordHasherService passwordHasher,
        ILogger<AuthController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    [Route("/Auth/Login")]
    [Route("/Auth/Auth/Login")]
    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToRoleDashboard();

        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    [Route("/Auth/Login")]
    [Route("/Auth/Auth/Login")]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var normalizedUsername = model.Username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername || u.Email == normalizedUsername);

        if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, model.Password))
        {
            _logger.LogWarning("Đăng nhập thất bại: Sai thông tin đăng nhập cho '{Username}'", model.Username);
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không chính xác.");
            return View(model);
        }

        // BẢNG 7.2 TC-01: Chặn đăng nhập tài khoản có trạng thái Inactive
        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("Đăng nhập bị từ chối: Tài khoản '{Username}' (ID: {UserId}) đang ở trạng thái Inactive", user.Username, user.Id);
            ModelState.AddModelError(string.Empty, "Tài khoản của bạn đã bị vô hiệu hóa. Vui lòng liên hệ Quản trị viên.");

            var failedLog = SystemLog.Create(
                action: "Login Denied",
                details: $"Đăng nhập thất bại do tài khoản bị vô hiệu hóa: {user.Username}",
                userId: user.Id,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            if (failedLog.IsSuccess)
            {
                await _dbContext.SystemLogs.AddAsync(failedLog.Value);
                await _dbContext.SaveChangesAsync();
            }

            return View(model);
        }

        // Cấp phiên làm việc Cookie Authentication
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

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        var successLog = SystemLog.Create(
            action: "Login Success",
            details: $"Người dùng {user.Username} đăng nhập thành công với vai trò {user.Role}.",
            userId: user.Id,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (successLog.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(successLog.Value);
            await _dbContext.SaveChangesAsync();
        }

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
            return Redirect(model.ReturnUrl);

        return RedirectToRoleDashboard(user.Role);
    }

    [HttpPost, HttpGet]
    [Route("/Auth/Logout")]
    [Route("/Auth/Auth/Logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout()
    {
        var userId = CurrentUserId;
        var username = CurrentUserName;

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (userId.HasValue)
        {
            var log = SystemLog.Create(
                action: "Logout",
                details: $"Người dùng {username} đã đăng xuất an toàn.",
                userId: userId.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            if (log.IsSuccess)
            {
                await _dbContext.SystemLogs.AddAsync(log.Value);
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

        if (!CurrentUserId.HasValue)
            return RedirectToAction(nameof(Login));

        var user = await _dbContext.Users.FindAsync(CurrentUserId.Value);
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

        var log = SystemLog.Create(
            action: "Change Password",
            details: $"Người dùng {user.Username} đã đổi mật khẩu cá nhân thành công.",
            userId: user.Id,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess)
        {
            await _dbContext.SystemLogs.AddAsync(log.Value);
        }

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
        var roleName = role?.ToString() ?? CurrentUserRole;
        return roleName switch
        {
            RoleNames.Admin  => RedirectToAction("Dashboard", "Admin"),
            RoleNames.Leader => RedirectToAction("Dashboard", "Leader"),
            RoleNames.Staff  => RedirectToAction("Dashboard", "Staff"),
            _                => RedirectToAction("Index", "Ticket")
        };
    }
}
