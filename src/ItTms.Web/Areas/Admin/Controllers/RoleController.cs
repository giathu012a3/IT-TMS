using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ItTms.Application.Common.Interfaces;
using ItTms.Domain.Enums;

namespace ItTms.Web.Areas.Admin.Controllers;

public class RoleController : AdminBaseController
{
    private readonly IItTmsDbContext _dbContext;

    public RoleController(IItTmsDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
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
}
