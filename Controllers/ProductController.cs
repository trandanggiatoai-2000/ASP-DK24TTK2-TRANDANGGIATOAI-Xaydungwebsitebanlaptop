using Microsoft.AspNetCore.Mvc;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Controllers;

public class ProductController : Controller
{
    private readonly IStorefrontService _storefrontService;
    private readonly IFileStorageService _fileStorageService;

    public ProductController(IStorefrontService storefrontService, IFileStorageService fileStorageService)
    {
        _storefrontService = storefrontService;
        _fileStorageService = fileStorageService;
    }

    public async Task<IActionResult> Catalog(string? keyword, string? brand, string? category, string? cpu, string? ram, string? ssd, bool official = false, bool fast = false, bool installment = false, int page = 1, bool ajax = false)
    {
        var vm = await _storefrontService.SearchProductsAsync(keyword, brand, category, cpu, ram, ssd, official, fast, installment, page, 20);
        ViewBag.WebsiteSettings = await _storefrontService.GetWebsiteSettingsAsync();
        if (ajax || string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase))
            return PartialView("_CatalogResults", vm);
        return View(vm);
    }

    public async Task<IActionResult> Details(int id, int? star = null)
    {
        var vm = await _storefrontService.GetProductByIdAsync(id, star);
        if (vm == null) return NotFound();
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ActionName("PostReview")]
    public async Task<IActionResult> PostReview(int id, string reviewerName, int rating, string comment, IFormFile? reviewImage)
    {
        if (string.IsNullOrWhiteSpace(comment))
        {
            TempData["CartMessage"] = "Vui lòng nhập nội dung bình luận.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var imageUrl = await _fileStorageService.SaveAsync(reviewImage, "reviews");
        await _storefrontService.AddProductReviewAsync(id, reviewerName, rating, comment, imageUrl ?? string.Empty);
        TempData["CartMessage"] = "Đã gửi đánh giá và bình luận cho sản phẩm.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> AddReview(int productId, string reviewerName, int rating, string commentText, IFormFile? reviewImage)
        => PostReview(productId, reviewerName, rating, commentText, reviewImage);
}

