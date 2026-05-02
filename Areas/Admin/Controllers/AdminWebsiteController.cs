using websitebanlaptop.Extensions;
using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminWebsiteController : AdminControllerBase
{
    private readonly IAdminContentService _adminContentService;
    private readonly IAdminMediaService _adminMediaService;

    public AdminWebsiteController(IAdminContentService adminContentService, IAdminMediaService adminMediaService)
    {
        _adminContentService = adminContentService;
        _adminMediaService = adminMediaService;
    }

    private IActionResult Denied()
    {
        TempData["CartMessage"] = "Bạn không có quyền chỉnh sửa giao diện website.";
        return RedirectToAction("Index", "AdminInventory", new { area = "Admin" });
    }

    public async Task<IActionResult> Index()
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageWebsite()) return Denied();
        return View(await _adminContentService.GetWebsitePageAsync());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminWebsitePageViewModel model)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageWebsite()) return Denied();
        await _adminMediaService.ApplyWebsiteAssetsAsync(model);

        await _adminContentService.SaveWebsitePresentationAsync(model);
        await _adminContentService.SavePolicyPagesAsync(model.AboutPolicies);
        TempData["CartMessage"] = "Đã cập nhật giao diện website thành công.";
        return RedirectToAction(nameof(Index));
    }
}

