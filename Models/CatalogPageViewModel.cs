namespace websitebanlaptop.Models;

public class CatalogPageViewModel
{
    public List<ProductCardViewModel> Products { get; set; } = new();
    public WebsiteSettingsViewModel WebsiteSettings { get; set; } = new();
    public List<string> Brands { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Cpus { get; set; } = new();
    public List<string> Rams { get; set; } = new();
    public List<string> Ssds { get; set; } = new();
    public string? Keyword { get; set; }
    public string? Brand { get; set; }
    public string? Category { get; set; }
    public string? Cpu { get; set; }
    public string? Ram { get; set; }
    public string? Ssd { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public bool Official { get; set; }
    public bool FastDelivery { get; set; }
    public bool Installment { get; set; }
    public int TotalItems { get; set; }
    public int TotalPages => PageSize == 0 ? 1 : (int)Math.Ceiling((double)TotalItems / PageSize);
    public int StartItem => TotalItems == 0 ? 0 : ((Page - 1) * PageSize) + 1;
    public int EndItem => Math.Min(Page * PageSize, TotalItems);
    public int RemainingItems => Math.Max(0, TotalItems - EndItem);
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}


