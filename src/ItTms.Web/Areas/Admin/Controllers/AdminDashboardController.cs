using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Entities;
using ItTms.Domain.Enums;
using ItTms.Web.Models.ViewModels;

namespace ItTms.Web.Areas.Admin.Controllers;

public class AdminDashboardController : AdminBaseController
{
    private readonly IItTmsDbContext _dbContext;

    public AdminDashboardController(IItTmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IActionResult> Index()
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
}
