using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Web.Models.ViewModels;
using DomainUser = ItTms.Domain.Entities.User;

namespace ItTms.Web.Areas.Admin.Controllers;

public class UserController : AdminBaseController
{
    private readonly IItTmsDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<UserController> _logger;

    public UserController(
        IItTmsDbContext dbContext,
        IPasswordHasherService passwordHasher,
        ILogger<UserController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, UserRole? role, UserStatus? status)
    {
        var query = _dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLowerInvariant();
            query = query.Where(u => u.Username.ToLower().Contains(s) || 
                                     u.FullName.ToLower().Contains(s) || 
                                     u.Email.ToLower().Contains(s) ||
                                     (u.Department != null && u.Department.ToLower().Contains(s)));
        }

        if (role.HasValue)
        {
            query = query.Where(u => u.Role == role.Value);
        }

        if (status.HasValue)
        {
            query = query.Where(u => u.Status == status.Value);
        }

        var users = await query
            .OrderBy(u => u.Id)
            .Select(u => new UserItemViewModel
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                Status = u.Status,
                Department = u.Department,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        ViewBag.CurrentSearch = search;
        ViewBag.CurrentRole = role;
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentAdminId = CurrentUserId;

        return View(users);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var normalizedUsername = model.Username.Trim().ToLowerInvariant();
        var normalizedEmail = model.Email.Trim().ToLowerInvariant();

        var exists = await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername || u.Email == normalizedEmail);
        if (exists)
        {
            ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc Email này đã tồn tại trong hệ thống.");
            return View(model);
        }

        var hash = _passwordHasher.HashPassword(model.Password);
        var userResult = DomainUser.Create(
            username: model.Username,
            email: model.Email,
            passwordHash: hash,
            fullName: model.FullName,
            role: model.Role,
            department: model.Department,
            status: UserStatus.Active
        );

        if (userResult.IsFailure)
        {
            ModelState.AddModelError(string.Empty, userResult.Error.Description);
            return View(model);
        }

        await _dbContext.Users.AddAsync(userResult.Value);

        var log = SystemLog.Create(
            action: "Admin Created User",
            details: $"Admin đã tạo tài khoản mới: {model.Username} ({model.Role})",
            userId: CurrentUserId!.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Tài khoản {model.Username} đã được khởi tạo thành công.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        var model = new EditUserViewModel
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            Role = user.Role,
            Department = user.Department
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _dbContext.Users.FindAsync(model.Id);
        if (user == null) return NotFound();

        // Cập nhật Profile và Role
        var profileResult = user.UpdateProfile(model.FullName, model.Department);
        if (profileResult.IsFailure)
        {
            ModelState.AddModelError(string.Empty, profileResult.Error.Description);
            return View(model);
        }

        user.ChangeRole(model.Role);

        var log = SystemLog.Create(
            action: "Admin Updated User",
            details: $"Admin đã cập nhật thông tin người dùng: {user.Username} (Role: {model.Role})",
            userId: CurrentUserId!.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Cập nhật tài khoản {user.Username} thành công.";
        return RedirectToAction(nameof(Index));
    }

    // BẢNG 7.2 TC-10: Admin kích hoạt/vô hiệu hóa tài khoản khác; CHẶN tự vô hiệu hóa chính mình!
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        // RÀNG BUỘC BẤT BIẾN TC-10: Chặn Admin tự vô hiệu hóa tài khoản của chính mình
        if (id == CurrentUserId!.Value)
        {
            _logger.LogWarning("TC-10 Blocked: Admin #{AdminId} cố tình tự vô hiệu hóa tài khoản của chính mình", id);
            TempData["ErrorMessage"] = "Quản trị viên không thể tự vô hiệu hóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Index));
        }

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.Status == UserStatus.Active)
        {
            var deactResult = user.Deactivate(CurrentUserId.Value);
            if (deactResult.IsFailure)
            {
                TempData["ErrorMessage"] = deactResult.Error.Description;
                return RedirectToAction(nameof(Index));
            }

            var log = SystemLog.Create(
                action: "Admin Deactivated User",
                details: $"Admin đã vô hiệu hóa tài khoản {user.Username} (ID: {user.Id})",
                userId: CurrentUserId.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

            TempData["SuccessMessage"] = $"Đã vô hiệu hóa tài khoản {user.Username}.";
        }
        else
        {
            user.Activate();

            var log = SystemLog.Create(
                action: "Admin Activated User",
                details: $"Admin đã kích hoạt lại tài khoản {user.Username} (ID: {user.Id})",
                userId: CurrentUserId.Value,
                ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
            );
            if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

            TempData["SuccessMessage"] = $"Đã kích hoạt lại tài khoản {user.Username}.";
        }

        await _dbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới không hợp lệ (tối thiểu 6 ký tự).";
            return RedirectToAction(nameof(Index));
        }

        var user = await _dbContext.Users.FindAsync(model.UserId);
        if (user == null) return NotFound();

        var newHash = _passwordHasher.HashPassword(model.NewPassword);
        user.UpdatePassword(newHash);

        var log = SystemLog.Create(
            action: "Admin Reset Password",
            details: $"Admin đã đặt lại mật khẩu cho tài khoản {user.Username}",
            userId: CurrentUserId!.Value,
            ipAddress: HttpContext.Connection.RemoteIpAddress?.ToString()
        );
        if (log.IsSuccess) await _dbContext.SystemLogs.AddAsync(log.Value);

        await _dbContext.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu mới cho tài khoản {user.Username}.";
        return RedirectToAction(nameof(Index));
    }
}
