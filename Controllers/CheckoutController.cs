using Microsoft.AspNetCore.Mvc;
using websitebanlaptop.Models;
using websitebanlaptop.Services;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Controllers;

public class CheckoutController : Controller
{
    private readonly CartSessionService _cartService;
    private readonly IStorefrontService _storefrontService;

    public CheckoutController(CartSessionService cartService, IStorefrontService storefrontService)
    {
        _cartService = cartService;
        _storefrontService = storefrontService;
    }

    public IActionResult Index()
    {
        var cart = new CartPageViewModel { Items = _cartService.GetCart() };
        if (!cart.Items.Any()) return RedirectToAction("Index", "Cart");
        var vm = new CheckoutPageViewModel { Cart = cart };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckoutPageViewModel model)
    {
        var cart = new CartPageViewModel { Items = _cartService.GetCart() };
        if (!cart.Items.Any()) return RedirectToAction("Index", "Cart");

        model.Cart = cart;

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var form = model.Form;
        var orderCode = await _storefrontService.CreateOrderAsync(form, cart.Items);
        _cartService.Clear();

        var isBankTransfer = string.Equals(form.PaymentMethod, "BankTransfer", StringComparison.OrdinalIgnoreCase);
        TempData["OrderCode"] = orderCode;
        TempData["PaymentMethod"] = isBankTransfer ? "Chuyển khoản ngân hàng" : "Thanh toán sau";
        TempData["PaymentStatusText"] = isBankTransfer ? "Đã thanh toán" : "Chưa thanh toán";
        TempData["SuccessTitle"] = isBankTransfer ? "Bạn đã thanh toán thành công." : "Bạn đã đặt hàng thành công.";
        TempData["SuccessMessage"] = isBankTransfer
            ? "Bạn đã đặt hàng thành công, sẽ có nhân viên liên hệ để xác nhận thông tin. Cảm ơn quý Khách hàng."
            : "Sẽ có nhân viên liên hệ để xác nhận thông tin. Cảm ơn quý Khách hàng.";

        return RedirectToAction(nameof(Popup));
    }

    public IActionResult Popup()
    {
        ViewBag.OrderCode = TempData.Peek("OrderCode")?.ToString() ?? string.Empty;
        ViewBag.PaymentMethod = TempData.Peek("PaymentMethod")?.ToString() ?? string.Empty;
        ViewBag.PaymentStatusText = TempData.Peek("PaymentStatusText")?.ToString() ?? string.Empty;
        ViewBag.SuccessTitle = TempData.Peek("SuccessTitle")?.ToString() ?? "Bạn đã đặt hàng thành công.";
        ViewBag.SuccessMessage = TempData.Peek("SuccessMessage")?.ToString() ?? "Sẽ có nhân viên liên hệ để xác nhận thông tin. Cảm ơn quý Khách hàng.";
        return View();
    }

    public IActionResult Success()
    {
        ViewBag.OrderCode = TempData["OrderCode"]?.ToString() ?? string.Empty;
        ViewBag.PaymentMethod = TempData["PaymentMethod"]?.ToString() ?? string.Empty;
        ViewBag.SuccessTitle = TempData["SuccessTitle"]?.ToString() ?? "Bạn đã đặt hàng thành công.";
        ViewBag.SuccessMessage = TempData["SuccessMessage"]?.ToString() ?? "Sẽ có nhân viên liên hệ để xác nhận thông tin. Cảm ơn quý Khách hàng.";
        ViewBag.PaymentStatusText = TempData["PaymentStatusText"]?.ToString() ?? "Chưa thanh toán";
        return View();
    }
}

