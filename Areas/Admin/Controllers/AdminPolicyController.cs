using websitebanlaptop.Extensions;
using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminPolicyController : AdminControllerBase
{
    private readonly IAdminContentService _adminContentService;

    public AdminPolicyController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    private IActionResult Denied()
    {
        TempData["CartMessage"] = "Bạn không có quyền quản lý nội dung hỗ trợ.";
        return RedirectToAction("Index", "AdminInventory", new { area = "Admin" });
    }

    public async Task<IActionResult> Index()
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageWebsite()) return Denied();

        return View(new AdminPolicyPageViewModel
        {
            Policies = await _adminContentService.GetPolicyPagesAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminPolicyPageViewModel model)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageWebsite()) return Denied();

        model.Policies ??= new();
        await _adminContentService.SavePolicyPagesAsync(model.Policies);
        TempData["CartMessage"] = "Đã cập nhật nội dung hỗ trợ thành công.";
        return RedirectToAction(nameof(Index));
    }
}

