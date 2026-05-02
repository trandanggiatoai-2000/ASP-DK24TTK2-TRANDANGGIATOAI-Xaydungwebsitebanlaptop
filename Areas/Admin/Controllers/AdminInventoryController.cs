using websitebanlaptop.Extensions;
using websitebanlaptop.Models;
using websitebanlaptop.Services;
using websitebanlaptop.Services.Contracts;
using Microsoft.AspNetCore.Mvc;

namespace websitebanlaptop.Areas.Admin.Controllers;

[Area("Admin")]

public class AdminInventoryController : AdminControllerBase
{
    private readonly IAdminInventoryService _adminInventoryService;
    private readonly IAdminMediaService _adminMediaService;
    private readonly IFileStorageService _fileStorageService;

    public AdminInventoryController(
        IAdminInventoryService adminInventoryService,
        IAdminMediaService adminMediaService,
        IFileStorageService fileStorageService)
    {
        _adminInventoryService = adminInventoryService;
        _adminMediaService = adminMediaService;
        _fileStorageService = fileStorageService;
    }

    private bool CanManageInventory() => HttpContext.Session.CanManageInventory();
    private bool CanDeleteInventory() => HttpContext.Session.CanDeleteInventory();
    private bool CanImportInventory() => HttpContext.Session.CanImportInventory();

    public async Task<IActionResult> Index(string? keyword = null, string? brand = null, string? category = null)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();

        var pageData = await _adminInventoryService.GetInventoryPageDataAsync(keyword, brand, category);
        ViewBag.Summary = await _adminInventoryService.GetSummaryAsync();
        ViewBag.CanManageInventory = CanManageInventory();
        ViewBag.CanDeleteInventory = CanDeleteInventory();
        ViewBag.CanImportInventory = CanImportInventory();
        ViewBag.InventoryKeyword = keyword ?? string.Empty;
        ViewBag.InventoryBrand = brand ?? string.Empty;
        ViewBag.InventoryCategory = category ?? string.Empty;
        ViewBag.InventoryBrands = pageData.Brands;
        ViewBag.InventoryCategories = pageData.Categories;

        return View(pageData.Items);
    }

    [HttpGet]
    public IActionResult Create(string? returnUrl = null)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanManageInventory()) return RedirectDenied("Tài khoản này không có quyền thêm hoặc chỉnh sửa thiết bị.");
        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl) ?? GetSafeReferrer() ?? Url.Action(nameof(Index), "AdminInventory", new { area = "Admin" });
        return View("Form", new InventoryFormViewModel { IsFeatured = true, DiscountPercent = 0, SortOrder = 0 });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(InventoryFormViewModel model, string? returnUrl = null)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanManageInventory()) return RedirectDenied("Tài khoản này không có quyền thêm hoặc chỉnh sửa thiết bị.");
        await _adminMediaService.ApplyInventoryImagesAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl) ?? Url.Action(nameof(Index), "AdminInventory", new { area = "Admin" });
            return View("Form", model);
        }
        var createdId = await _adminInventoryService.CreateProductAsync(model);
        TempData["CartMessage"] = "Đã thêm sản phẩm mới vào kho.";
        var safeReturnUrl = NormalizeReturnUrl(returnUrl);
        return RedirectToAction(nameof(Edit), new { id = createdId, returnUrl = safeReturnUrl });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? returnUrl = null)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanManageInventory()) return RedirectDenied("Tài khoản này không có quyền chỉnh sửa thiết bị.");
        var model = await _adminInventoryService.GetInventoryFormByIdAsync(id);
        if (model == null) return RedirectToAction(nameof(Index));
        ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl) ?? GetSafeReferrer() ?? Url.Action(nameof(Index), "AdminInventory", new { area = "Admin" });
        return View("Form", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(InventoryFormViewModel model, string? returnUrl = null)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanManageInventory()) return RedirectDenied("Tài khoản này không có quyền chỉnh sửa thiết bị.");
        await _adminMediaService.ApplyInventoryImagesAsync(model);
        if (!ModelState.IsValid)
        {
            ViewBag.ReturnUrl = NormalizeReturnUrl(returnUrl) ?? Url.Action(nameof(Index), "AdminInventory", new { area = "Admin" });
            return View("Form", model);
        }
        await _adminInventoryService.UpdateProductAsync(model);
        TempData["CartMessage"] = "Đã cập nhật sản phẩm.";
        var safeReturnUrl = NormalizeReturnUrl(returnUrl);
        return RedirectToAction(nameof(Edit), new { id = model.ProductId, returnUrl = safeReturnUrl });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStock(int productId, int stockQty)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanManageInventory()) return RedirectDenied("Tài khoản này không có quyền cập nhật tồn kho.");
        await _adminInventoryService.UpdateStockAsync(productId, stockQty);
        TempData["CartMessage"] = "Đã cập nhật tồn kho.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Import()
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanImportInventory()) return RedirectDenied("Tài khoản này không có quyền import thiết bị hoặc danh mục.");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Import(IFormFile excelFile)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanImportInventory()) return RedirectDenied("Tài khoản này không có quyền import thiết bị hoặc danh mục.");
        if (excelFile == null || excelFile.Length == 0)
        {
            TempData["CartMessage"] = "Vui lòng chọn file Excel hoặc CSV để import.";
            return RedirectToAction(nameof(Import));
        }

        var rows = await ExcelProductImportService.ReadRowsAsync(excelFile);
        var count = await _adminInventoryService.ImportProductsAsync(rows);
        TempData["CartMessage"] = $"Đã import {count} sản phẩm vào kho.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult DownloadTemplate()
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanImportInventory()) return RedirectDenied("Tài khoản này không có quyền tải mẫu import.");
        var path = _fileStorageService.GetTemplatePhysicalPath("templates", "product_import_template.xlsx");
        return PhysicalFile(path, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "product_import_template.xlsx");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsLoggedIn()) return RedirectToAdminLogin();
        if (!CanDeleteInventory()) return RedirectDenied("Tài khoản này không có quyền xóa thiết bị.");
        await _adminInventoryService.DeleteProductAsync(id);
        TempData["CartMessage"] = "Đã xóa sản phẩm khỏi kho.";
        return RedirectToAction(nameof(Index));
    }

    private string? NormalizeReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl)) return null;
        if (Url.IsLocalUrl(returnUrl)) return returnUrl;
        return null;
    }

    private string? GetSafeReferrer()
    {
        var referer = Request.Headers.Referer.ToString();
        if (string.IsNullOrWhiteSpace(referer)) return null;
        if (Uri.TryCreate(referer, UriKind.Absolute, out var uri))
        {
            var local = uri.PathAndQuery + uri.Fragment;
            return Url.IsLocalUrl(local) ? local : null;
        }
        return Url.IsLocalUrl(referer) ? referer : null;
    }

}

