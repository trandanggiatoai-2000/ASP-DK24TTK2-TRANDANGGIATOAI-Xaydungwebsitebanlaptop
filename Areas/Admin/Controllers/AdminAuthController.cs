using websitebanlaptop.Extensions;
using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminAuthController : Controller
{
    private readonly IAdminAuthService _adminAuthService;

    public AdminAuthController(IAdminAuthService adminAuthService)
    {
        _adminAuthService = adminAuthService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsAdminAuthenticated())
            return RedirectToLocal(returnUrl);

        ViewBag.ReturnUrl = returnUrl;
        return View(new AdminLoginViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid) return View(model);

        var user = await _adminAuthService.ValidateLoginAsync(model.Username, model.Password);
        if (user != null)
        {
            HttpContext.Session.SignInAdmin(user);
            TempData["CartMessage"] = "Đăng nhập quản trị thành công.";
            return RedirectToLocal(returnUrl);
        }

        model.ErrorMessage = "Sai tài khoản hoặc mật khẩu.";
        return View(model);
    }

    private IActionResult RedirectToLocal(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        return RedirectToAction("Index", "AdminInventory", new { area = "Admin" });
    }

    public IActionResult Logout()
    {
        HttpContext.Session.SignOutAdmin();
        TempData["CartMessage"] = "Bạn đã đăng xuất quản trị.";
        return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
    }
}

