namespace ItTms.Controllers;

public class HomeController : BaseController
{
    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated != true)
            return RedirectToAction("Login", "Auth");

        return CurrentUserRole switch
        {
            RoleNames.Admin => RedirectToAction("Dashboard", "Admin"),
            RoleNames.Leader => RedirectToAction("Dashboard", "Leader"),
            RoleNames.Staff => RedirectToAction("Dashboard", "Staff"),
            _ => RedirectToAction("Index", "Ticket")
        };
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = System.Diagnostics.Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
