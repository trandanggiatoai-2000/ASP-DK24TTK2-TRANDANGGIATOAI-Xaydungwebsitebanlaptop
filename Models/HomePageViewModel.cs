namespace websitebanlaptop.Models;

public class HomePageViewModel
{
    public WebsiteSettingsViewModel Settings { get; set; } = new();
    public List<BannerSlide> Slides { get; set; } = new();
    public List<string> FeaturedBrands { get; set; } = new();
    public List<string> DemandGroups { get; set; } = new();
    public List<ProductCardViewModel> FeaturedProducts { get; set; } = new();
    public List<ProductCardViewModel> FlashSaleProducts { get; set; } = new();
}

