namespace websitebanlaptop.Models;

public class InventoryImportRow
{
    public string Brand { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public string Ssd { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public int StockQty { get; set; }
    public bool IsFeatured { get; set; }
    public string BadgeText { get; set; } = "Giảm sốc";
    public string DescriptionText { get; set; } = string.Empty;
    public int DiscountPercent { get; set; }
    public int SortOrder { get; set; }
    public string Image1 { get; set; } = string.Empty;
    public string? Image2 { get; set; }
    public string? Image3 { get; set; }
}

