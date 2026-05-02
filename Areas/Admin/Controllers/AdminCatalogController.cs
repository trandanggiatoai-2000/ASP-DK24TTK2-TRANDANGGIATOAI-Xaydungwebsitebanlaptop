using websitebanlaptop.Extensions;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminCatalogController : AdminControllerBase
{
    private readonly IAdminContentService _adminContentService;

    public AdminCatalogController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    public async Task<IActionResult> Index(string? brand, string? category, string? keyword)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        var vm = await _adminContentService.GetCatalogAsync(brand, category, keyword);
        ViewBag.CanImportInventory = HttpContext.Session.CanImportInventory();
        ViewBag.CanManageInventory = HttpContext.Session.CanManageInventory();
        return View(vm);
    }
}

