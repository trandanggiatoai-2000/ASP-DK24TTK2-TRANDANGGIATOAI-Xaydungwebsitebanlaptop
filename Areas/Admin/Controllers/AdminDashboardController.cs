using websitebanlaptop.Extensions;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminDashboardController : AdminControllerBase
{
    private readonly IAdminContentService _adminContentService;

    public AdminDashboardController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    public async Task<IActionResult> Index()
    {
        if (!HttpContext.Session.IsAdminAuthenticated())
            return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });

        var vm = await _adminContentService.GetDashboardAsync();
        return View(vm);
    }
}

