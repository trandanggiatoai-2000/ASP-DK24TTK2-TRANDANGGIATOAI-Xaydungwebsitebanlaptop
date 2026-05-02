using websitebanlaptop.Extensions;
using Microsoft.AspNetCore.Mvc;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminSalesController : AdminControllerBase
{
    private readonly IAdminOrderService _adminOrderService;

    public AdminSalesController(IAdminOrderService adminOrderService)
    {
        _adminOrderService = adminOrderService;
    }

    private IActionResult Denied(string message)
    {
        TempData["CartMessage"] = message;
        return RedirectToAction("Index", "AdminInventory", new { area = "Admin" });
    }

    public async Task<IActionResult> Index(string status = "all")
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanViewOrders()) return Denied("Tài khoản này không có quyền xem đơn hàng.");

        ViewBag.Status = status;
        ViewBag.CanUpdateOrders = HttpContext.Session.CanUpdateOrders();
        ViewBag.CanCancelOrders = HttpContext.Session.CanCancelOrders();
        var vm = await _adminOrderService.GetOrdersAsync(status);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id, string print = "")
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanViewOrders()) return Denied("Tài khoản này không có quyền xem đơn hàng.");

        var vm = await _adminOrderService.GetOrderDetailAsync(id);
        if (vm == null) return RedirectToAction(nameof(Index));

        ViewBag.CanUpdateOrders = HttpContext.Session.CanUpdateOrders();
        ViewBag.CanCancelOrders = HttpContext.Session.CanCancelOrders();

        if (string.Equals(print, "invoice", StringComparison.OrdinalIgnoreCase))
            return View("PrintInvoice", vm);

        if (string.Equals(print, "delivery", StringComparison.OrdinalIgnoreCase))
            return View("PrintDelivery", vm);

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MarkDelivered(int orderId)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanUpdateOrders()) return Denied("Tài khoản này không có quyền cập nhật trạng thái đơn hàng.");

        await _adminOrderService.MarkDeliveredAsync(orderId);
        TempData["CartMessage"] = "Đã chuyển trạng thái đơn hàng.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int orderId)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanCancelOrders()) return Denied("Tài khoản này không có quyền hủy đơn hàng.");

        await _adminOrderService.CancelAsync(orderId);
        TempData["CartMessage"] = "Đã hủy đơn và hoàn lại kho nếu đơn đã trừ tồn.";
        return RedirectToAction(nameof(Index));
    }
}

