using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IStorefrontRepository
{
    Task<HomePageViewModel> GetHomePageAsync();
    Task<List<BannerSlide>> GetSlidesAsync();
    Task<CatalogPageViewModel> SearchProductsAsync(string? keyword, string? brand, string? category, string? cpu, string? ram, string? ssd, bool official, bool fast, bool installment, int page, int pageSize);
    Task<ProductDetailsViewModel?> GetProductByIdAsync(int id, int? star = null);
    Task<List<ProductReviewViewModel>> GetProductReviewsAsync(int productId, int? star = null);
    Task AddProductReviewAsync(int productId, string reviewerName, int rating, string commentText, string imageUrl);
    Task<List<string>> GetProductImagesAsync(int productId);
    Task<string> CreateOrderAsync(CheckoutViewModel form, List<CartItem> items);
}

