using System.Diagnostics;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ItTms.Domain.Enums;
using ItTms.Web.Models;

namespace ItTms.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            return role switch
            {
                RoleNames.Admin  => RedirectToAction("Index", "AdminDashboard", new { area = "Admin" }),
                RoleNames.Leader => RedirectToAction("Index", "LeaderDashboard", new { area = "Leader" }),
                RoleNames.Staff  => RedirectToAction("Index", "Dashboard", new { area = "Staff" }),
                _                => RedirectToAction("Index", "Dashboard", new { area = "User" })
            };
        }

        return RedirectToAction("Login", "Auth", new { area = "Auth" });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
