using Microsoft.AspNetCore.Mvc;
using websitebanlaptop.Models;
using websitebanlaptop.Services;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Controllers;

public class CartController : Controller
{
    private readonly IStorefrontService _storefrontService;
    private readonly CartSessionService _cartService;

    public CartController(IStorefrontService storefrontService, CartSessionService cartService)
    {
        _storefrontService = storefrontService;
        _cartService = cartService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1)
    {
        var product = await _storefrontService.GetProductByIdAsync(productId);
        if (product != null)
            _cartService.AddItem(product, quantity <= 0 ? 1 : quantity);
        TempData["CartMessage"] = "Đã thêm sản phẩm vào giỏ hàng.";
        return Redirect(Request.Headers["Referer"].ToString());
    }

    public IActionResult Index()
    {
        var vm = new CartPageViewModel { Items = _cartService.GetCart() };
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Update(int productId, int quantity)
    {
        _cartService.UpdateQuantity(productId, quantity);
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Remove(int productId)
    {
        _cartService.Remove(productId);
        return RedirectToAction(nameof(Index));
    }
}

