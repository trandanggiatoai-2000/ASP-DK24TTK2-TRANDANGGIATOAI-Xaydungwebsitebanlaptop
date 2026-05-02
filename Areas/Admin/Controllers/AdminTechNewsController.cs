using websitebanlaptop.Extensions;
using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminTechNewsController : AdminControllerBase
{
    private readonly IAdminContentService _adminContentService;

    public AdminTechNewsController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    private IActionResult Denied()
    {
        TempData["CartMessage"] = "Bạn không có quyền quản lý tin công nghệ.";
        return RedirectToAction("Index", "AdminInventory", new { area = "Admin" });
    }

    public async Task<IActionResult> Index()
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageWebsite()) return Denied();

        return View(new AdminTechNewsPageViewModel
        {
            Posts = await _adminContentService.GetTechNewsPostsAsync()
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(AdminTechNewsPageViewModel model)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageWebsite()) return Denied();

        model.Posts ??= new();
        await _adminContentService.SaveTechNewsPostsAsync(model.Posts);
        TempData["CartMessage"] = "Đã cập nhật tin công nghệ thành công.";
        return RedirectToAction(nameof(Index));
    }
}

