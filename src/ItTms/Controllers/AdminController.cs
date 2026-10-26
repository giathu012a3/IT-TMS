namespace ItTms.Controllers;

[AuthorizeRoles(RoleNames.Admin)]
public class AdminController : BaseController
{
    private readonly ItTmsDbContext _dbContext;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        ItTmsDbContext dbContext,
        IPasswordHasherService passwordHasher,
        ILogger<AdminController> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    [HttpGet]
    [Route("/Admin/Dashboard")]
    [Route("/Admin")]
    public async Task<IActionResult> Dashboard()
    {
        var totalUsers = await _dbContext.Users.CountAsync();
        var activeUsers = await _dbContext.Users.CountAsync(u => u.Status == UserStatus.Active);
        var totalTickets = await _dbContext.Tickets.CountAsync();
        var totalLogs = await _dbContext.SystemLogs.CountAsync();

        var roleCounts = await _dbContext.Users
            .GroupBy(u => u.Role)
            .Select(g => new { Role = g.Key.ToString(), Count = g.Count() })
            .ToDictionaryAsync(x => x.Role, x => x.Count);

        var recentLogs = await _dbContext.SystemLogs
            .Include(l => l.User)
            .OrderByDescending(l => l.CreatedAt)
            .Take(10)
            .Select(l => new SystemLogItemViewModel
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User != null ? l.User.Username : "Hệ thống",
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        var viewModel = new AdminDashboardViewModel
        {
            TotalUsers = totalUsers,
            ActiveUsers = activeUsers,
            TotalTickets = totalTickets,
            TotalLogs = totalLogs,
            RoleCounts = roleCounts,
            RecentLogs = recentLogs
        };

        return View(viewModel);
    }

    [HttpGet]
    [Route("/Admin/Users")]
    [Route("/Admin/User")]
    public async Task<IActionResult> Users(string? search, UserRole? role, UserStatus? status)
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
    [Route("/Admin/CreateUser")]
    [Route("/Admin/User/Create")]
    public IActionResult CreateUser()
    {
        return View(new CreateUserViewModel());
    }

    [HttpPost]
    [Route("/Admin/CreateUser")]
    [Route("/Admin/User/Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateUser(CreateUserViewModel model)
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
        var userResult = ItTms.Models.Entities.User.Create(
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
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    public async Task<IActionResult> EditUser(int id)
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
    public async Task<IActionResult> EditUser(EditUserViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _dbContext.Users.FindAsync(model.Id);
        if (user == null) return NotFound();

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
        return RedirectToAction(nameof(Users));
    }

    // BẢNG 7.2 TC-10: Admin kích hoạt/vô hiệu hóa tài khoản khác; CHẶN tự vô hiệu hóa chính mình!
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        if (id == CurrentUserId!.Value)
        {
            _logger.LogWarning("TC-10 Blocked: Admin #{AdminId} cố tình tự vô hiệu hóa tài khoản của chính mình", id);
            TempData["ErrorMessage"] = "Quản trị viên không thể tự vô hiệu hóa tài khoản của chính mình.";
            return RedirectToAction(nameof(Users));
        }

        var user = await _dbContext.Users.FindAsync(id);
        if (user == null) return NotFound();

        if (user.Status == UserStatus.Active)
        {
            var deactResult = user.Deactivate(CurrentUserId.Value);
            if (deactResult.IsFailure)
            {
                TempData["ErrorMessage"] = deactResult.Error.Description;
                return RedirectToAction(nameof(Users));
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
        return RedirectToAction(nameof(Users));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["ErrorMessage"] = "Mật khẩu mới không hợp lệ (tối thiểu 6 ký tự).";
            return RedirectToAction(nameof(Users));
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
        return RedirectToAction(nameof(Users));
    }

    [HttpGet]
    [Route("/Admin/Roles")]
    [Route("/Admin/Role")]
    public async Task<IActionResult> Roles()
    {
        var usersByRole = await _dbContext.Users
            .AsNoTracking()
            .GroupBy(u => u.Role)
            .Select(g => new
            {
                Role = g.Key,
                TotalCount = g.Count(),
                ActiveCount = g.Count(u => u.Status == UserStatus.Active)
            })
            .ToListAsync();

        ViewBag.UsersByRole = usersByRole;
        return View();
    }

    [HttpGet]
    [Route("/Admin/Audit")]
    [Route("/Admin/AuditLog")]
    public async Task<IActionResult> Audit(string? actionType, int? userId, DateTime? fromDate, DateTime? toDate)
    {
        var query = _dbContext.SystemLogs
            .AsNoTracking()
            .Include(l => l.User)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(actionType))
        {
            var a = actionType.Trim().ToLowerInvariant();
            query = query.Where(l => l.Action.ToLower().Contains(a));
        }

        if (userId.HasValue)
        {
            query = query.Where(l => l.UserId == userId.Value);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(l => l.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            var endOfDay = toDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(l => l.CreatedAt <= endOfDay);
        }

        var logs = await query
            .OrderByDescending(l => l.CreatedAt)
            .Take(100)
            .Select(l => new SystemLogItemViewModel
            {
                Id = l.Id,
                UserId = l.UserId,
                UserName = l.User != null ? l.User.Username : "Hệ thống",
                Action = l.Action,
                Details = l.Details,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        var distinctActions = await _dbContext.SystemLogs
            .AsNoTracking()
            .Select(l => l.Action)
            .Distinct()
            .Take(20)
            .ToListAsync();

        ViewBag.DistinctActions = distinctActions;
        var model = new AuditFilterViewModel
        {
            ActionType = actionType,
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate,
            Logs = logs
        };

        return View(model);
    }
}
