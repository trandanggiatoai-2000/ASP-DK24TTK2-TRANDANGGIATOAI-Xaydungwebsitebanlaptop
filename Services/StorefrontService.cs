using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class StorefrontService : IStorefrontService
{
    private readonly IStorefrontRepository _storefrontRepository;
    private readonly IWebsiteRepository _websiteRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly ITechNewsRepository _techNewsRepository;
    public StorefrontService(IStorefrontRepository storefrontRepository, IWebsiteRepository websiteRepository, IPolicyRepository policyRepository, ITechNewsRepository techNewsRepository)
    {
        _storefrontRepository = storefrontRepository;
        _websiteRepository = websiteRepository;
        _policyRepository = policyRepository;
        _techNewsRepository = techNewsRepository;
    }

    public Task<HomePageViewModel> GetHomePageAsync() => _storefrontRepository.GetHomePageAsync();
    public Task<WebsiteSettingsViewModel> GetWebsiteSettingsAsync() => _websiteRepository.GetWebsiteSettingsAsync();
    public async Task<CatalogPageViewModel> GetCatalogPageAsync(CatalogQueryModel query, int pageSize)
    {
        var catalog = await _storefrontRepository.SearchProductsAsync(
            query.Keyword,
            query.Brand,
            query.Category,
            query.Cpu,
            query.Ram,
            query.Ssd,
            query.Official,
            query.Fast,
            query.Installment,
            query.Page,
            pageSize);

        catalog.WebsiteSettings = await _websiteRepository.GetWebsiteSettingsAsync();
        return catalog;
    }
    public Task<ProductDetailsViewModel?> GetProductByIdAsync(int id, int? star = null) => _storefrontRepository.GetProductByIdAsync(id, star);
    public Task<List<ProductReviewViewModel>> GetProductReviewsAsync(int productId, int? star = null) => _storefrontRepository.GetProductReviewsAsync(productId, star);
    public Task AddProductReviewAsync(int productId, string reviewerName, int rating, string commentText, string imageUrl) => _storefrontRepository.AddProductReviewAsync(productId, reviewerName, rating, commentText, imageUrl);
    public Task<string> CreateOrderAsync(CheckoutViewModel form, List<CartItem> items) => _storefrontRepository.CreateOrderAsync(form, items);
    public Task<List<PolicyPageViewModel>> GetPolicyPagesAsync() => _policyRepository.GetPolicyPagesAsync();
    public Task<PolicyPageViewModel?> GetPolicyByKeyAsync(string key) => _policyRepository.GetPolicyByKeyAsync(key);
    public Task<List<TechNewsPostViewModel>> GetTechNewsPostsAsync(int take = 50) => _techNewsRepository.GetTechNewsPostsAsync(take);
    public Task<TechNewsPostViewModel?> GetTechNewsPostBySlugAsync(string slug) => _techNewsRepository.GetTechNewsPostBySlugAsync(slug);
}

