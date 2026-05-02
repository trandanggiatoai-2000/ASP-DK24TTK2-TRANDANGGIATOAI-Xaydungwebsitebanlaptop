using websitebanlaptop.Extensions;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminReviewsController : AdminControllerBase
{
    private readonly IAdminContentService _adminContentService;

    public AdminReviewsController(IAdminContentService adminContentService)
    {
        _adminContentService = adminContentService;
    }

    private IActionResult Denied(string message)
    {
        TempData["CartMessage"] = message;
        return RedirectToAction("Index", "AdminInventory", new { area = "Admin" });
    }

    public async Task<IActionResult> Index(int? productId = null)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanViewReviews()) return Denied("Tài khoản này không có quyền xem bình luận.");
        ViewBag.CanReplyReviews = HttpContext.Session.CanReplyReviews();
        ViewBag.CanDeleteReviews = HttpContext.Session.CanDeleteReviews();
        var vm = await _adminContentService.GetReviewsAsync(productId);
        ViewBag.ProductId = productId;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Reply(int reviewId, string replyText)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanReplyReviews()) return Denied("Tài khoản này không có quyền trả lời bình luận.");
        if (string.IsNullOrWhiteSpace(replyText))
        {
            TempData["CartMessage"] = "Vui lòng nhập nội dung trả lời.";
            return RedirectToAction(nameof(Index));
        }
        await _adminContentService.ReplyReviewAsync(reviewId, replyText);
        TempData["CartMessage"] = "Đã trả lời bình luận.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int reviewId)
    {
        if (!HttpContext.Session.IsAdminAuthenticated()) return RedirectToAction("Login", "AdminAuth", new { area = "Admin" });
        if (!HttpContext.Session.CanDeleteReviews()) return Denied("Tài khoản này không có quyền xóa bình luận.");
        await _adminContentService.DeleteReviewAsync(reviewId);
        TempData["CartMessage"] = "Đã xóa bình luận.";
        return RedirectToAction(nameof(Index));
    }
}

