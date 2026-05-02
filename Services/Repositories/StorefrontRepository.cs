using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class StorefrontRepository : IStorefrontRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public StorefrontRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<HomePageViewModel> GetHomePageAsync() => _storeRepository.GetHomePageAsync();
    public Task<List<BannerSlide>> GetSlidesAsync() => _storeRepository.GetSlidesAsync();
    public Task<CatalogPageViewModel> SearchProductsAsync(string? keyword, string? brand, string? category, string? cpu, string? ram, string? ssd, bool official, bool fast, bool installment, int page, int pageSize)
        => _storeRepository.SearchProductsAsync(keyword, brand, category, cpu, ram, ssd, official, fast, installment, page, pageSize);
    public Task<ProductDetailsViewModel?> GetProductByIdAsync(int id, int? star = null) => _storeRepository.GetProductByIdAsync(id, star);
    public Task<List<ProductReviewViewModel>> GetProductReviewsAsync(int productId, int? star = null) => _storeRepository.GetProductReviewsAsync(productId, star);
    public Task AddProductReviewAsync(int productId, string reviewerName, int rating, string commentText, string imageUrl) => _storeRepository.AddProductReviewAsync(productId, reviewerName, rating, commentText, imageUrl);
    public Task<List<string>> GetProductImagesAsync(int productId) => _storeRepository.GetProductImagesAsync(productId);
    public Task<string> CreateOrderAsync(CheckoutViewModel form, List<CartItem> items) => _storeRepository.CreateOrderAsync(form, items);
}

