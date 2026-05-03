using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IStorefrontService
{
    Task<HomePageViewModel> GetHomePageAsync();
    Task<WebsiteSettingsViewModel> GetWebsiteSettingsAsync();
    Task<CatalogPageViewModel> SearchProductsAsync(string? keyword, string? brand, string? category, string? cpu, string? ram, string? ssd, bool official, bool fast, bool installment, int page, int pageSize);
    Task<ProductDetailsViewModel?> GetProductByIdAsync(int id, int? star = null);
    Task<List<ProductReviewViewModel>> GetProductReviewsAsync(int productId, int? star = null);
    Task AddProductReviewAsync(int productId, string reviewerName, int rating, string commentText, string imageUrl);
    Task<string> CreateOrderAsync(CheckoutViewModel form, List<CartItem> items);
    Task<List<PolicyPageViewModel>> GetPolicyPagesAsync();
    Task<PolicyPageViewModel?> GetPolicyByKeyAsync(string key);
    Task<List<TechNewsPostViewModel>> GetTechNewsPostsAsync(int take = 50);
    Task<TechNewsPostViewModel?> GetTechNewsPostBySlugAsync(string slug);
}

