using websitebanlaptop.Extensions;
using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminUsersController : AdminControllerBase
{
    private readonly IAdminUserService _adminUserService;

    public AdminUsersController(IAdminUserService adminUserService)
    {
        _adminUserService = adminUserService;
    }

    private IActionResult Denied()
    {
        TempData["CartMessage"] = "Bạn không có quyền quản lý tài khoản admin.";
        return RedirectToAction("Index", "AdminInventory", new { area = "Admin" });
    }

    public async Task<IActionResult> Index()
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageUsers()) return Denied();

        var vm = new AdminUsersPageViewModel
        {
            Users = await _adminUserService.GetUsersAsync(),
            Form = new AdminUserFormViewModel()
        };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AdminUsersPageViewModel page)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageUsers()) return Denied();

        if (string.IsNullOrWhiteSpace(page.Form.Password) || page.Form.Password.Trim().Length < 6)
            ModelState.AddModelError("Form.Password", "Mật khẩu tối thiểu 6 ký tự.");

        if (!ModelState.IsValid)
        {
            page.Users = await _adminUserService.GetUsersAsync();
            return View("Index", page);
        }

        try
        {
            await _adminUserService.CreateUserAsync(page.Form);
            TempData["CartMessage"] = "Đã tạo tài khoản quản trị mới và gán quyền thành công.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError("Form.Username", ex.Message);
            page.Users = await _adminUserService.GetUsersAsync();
            return View("Index", page);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int userId)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageUsers()) return Denied();
        var form = await _adminUserService.GetUserByIdAsync(userId);
        if (form == null) return RedirectToAction(nameof(Index));
        return View(form);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(AdminUserFormViewModel model)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageUsers()) return Denied();

        ModelState.Remove(nameof(AdminUserFormViewModel.Password));
        if (!ModelState.IsValid) return View(model);

        try
        {
            await _adminUserService.UpdateUserAsync(model);
            TempData["CartMessage"] = "Đã cập nhật tài khoản admin.";
            return RedirectToAction(nameof(Index));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(nameof(AdminUserFormViewModel.Username), ex.Message);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int userId, string newPassword)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageUsers()) return Denied();
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 6)
        {
            TempData["CartMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
            return RedirectToAction(nameof(Index));
        }

        await _adminUserService.ResetPasswordAsync(userId, newPassword);
        TempData["CartMessage"] = "Đã reset mật khẩu tài khoản.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int userId)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanManageUsers()) return Denied();

        await _adminUserService.ToggleStatusAsync(userId);
        TempData["CartMessage"] = "Đã cập nhật trạng thái tài khoản.";
        return RedirectToAction(nameof(Index));
    }
}

