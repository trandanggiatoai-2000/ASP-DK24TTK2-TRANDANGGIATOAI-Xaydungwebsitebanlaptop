namespace websitebanlaptop.Models;

public class AdminCatalogViewModel
{
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Keyword { get; set; }
    public List<string> Brands { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<InventoryItemViewModel> Items { get; set; } = new();
}

