namespace websitebanlaptop.Models;

public class InventoryItemViewModel
{
    public int ProductId { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public string Ssd { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int StockQty { get; set; }
    public bool IsFeatured { get; set; }
    public string Thumbnail { get; set; } = string.Empty;
}

