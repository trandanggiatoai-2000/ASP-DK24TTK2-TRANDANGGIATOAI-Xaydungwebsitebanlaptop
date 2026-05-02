using websitebanlaptop.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

public abstract class AdminControllerBase : Controller
{
    protected bool IsLoggedIn() => HttpContext.Session.IsAdminAuthenticated();
    protected IActionResult RedirectToAdminLogin() => RedirectToAction("Login", "AdminAuth", new { area = "Admin" });

    protected IActionResult RedirectDenied(string message, string action = "Index", string controller = "AdminInventory")
    {
        TempData["CartMessage"] = message;
        return RedirectToAction(action, controller, new { area = "Admin" });
    }
}

