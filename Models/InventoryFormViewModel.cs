using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace websitebanlaptop.Models;

public class InventoryFormViewModel
{
    public int ProductId { get; set; }

    [Required]
    public string Brand { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Danh mục")]
    public string CategoryName { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Tên sản phẩm")]
    public string ProductName { get; set; } = string.Empty;

    [Required]
    public string Cpu { get; set; } = string.Empty;

    [Required]
    public string Ram { get; set; } = string.Empty;

    [Required]
    public string Ssd { get; set; } = string.Empty;

    [Range(0, double.MaxValue)]
    public decimal Price { get; set; }

    [Range(0, double.MaxValue)]
    [Display(Name = "Giá cũ")]
    public decimal OldPrice { get; set; }

    [Range(0, int.MaxValue)]
    [Display(Name = "Tồn kho")]
    public int StockQty { get; set; }

    [Display(Name = "Nổi bật")]
    public bool IsFeatured { get; set; }

    [Display(Name = "Badge")]
    public string BadgeText { get; set; } = "Giảm sốc";

    [Display(Name = "Mô tả")]
    public string DescriptionText { get; set; } = string.Empty;

    [Range(0, 100)]
    [Display(Name = "Giảm giá %")]
    public int DiscountPercent { get; set; }

    [Display(Name = "Thứ tự")]
    public int SortOrder { get; set; }

    [Display(Name = "Danh sách URL ảnh")]
    public string ImageGalleryText { get; set; } = string.Empty;

    public List<string> ExistingImages { get; set; } = new();

    [Display(Name = "Upload nhiều ảnh")]
    public IFormFile[]? NewImageFiles { get; set; }
}


