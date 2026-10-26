using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Web.Models.ViewModels;

namespace ItTms.Web.Areas.Admin.Controllers;

public class AuditController : AdminBaseController
{
    private readonly IItTmsDbContext _dbContext;

    public AuditController(IItTmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? actionType, int? userId, DateTime? fromDate, DateTime? toDate)
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

        var model = new AuditFilterViewModel
        {
            ActionType = actionType,
            UserId = userId,
            FromDate = fromDate,
            ToDate = toDate,
            Logs = logs
        };

        ViewBag.DistinctActions = await _dbContext.SystemLogs
            .AsNoTracking()
            .Select(l => l.Action)
            .Distinct()
            .OrderBy(a => a)
            .ToListAsync();

        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var log = await _dbContext.SystemLogs
            .AsNoTracking()
            .Include(l => l.User)
            .FirstOrDefaultAsync(l => l.Id == id);

        if (log == null) return NotFound();

        return Json(new
        {
            log.Id,
            log.Action,
            log.Details,
            log.IpAddress,
            UserName = log.User?.Username ?? "Hệ thống",
            UserFullName = log.User?.FullName ?? "N/A",
            CreatedAt = log.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss")
        });
    }
}
