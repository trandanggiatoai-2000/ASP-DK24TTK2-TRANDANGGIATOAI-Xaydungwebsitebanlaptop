using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class AdminContentService : IAdminContentService
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IReviewManagementRepository _reviewRepository;
    private readonly IWebsiteRepository _websiteRepository;
    private readonly IPolicyRepository _policyRepository;
    private readonly ITechNewsRepository _techNewsRepository;

    public AdminContentService(
        IInventoryRepository inventoryRepository,
        IReviewManagementRepository reviewRepository,
        IWebsiteRepository websiteRepository,
        IPolicyRepository policyRepository,
        ITechNewsRepository techNewsRepository)
    {
        _inventoryRepository = inventoryRepository;
        _reviewRepository = reviewRepository;
        _websiteRepository = websiteRepository;
        _policyRepository = policyRepository;
        _techNewsRepository = techNewsRepository;
    }

    public Task<AdminCatalogViewModel> GetCatalogAsync(string? brand, string? category, string? keyword) => _inventoryRepository.GetAdminCatalogAsync(brand, category, keyword);
    public Task<List<AdminReviewItemViewModel>> GetReviewsAsync(int? productId = null) => _reviewRepository.GetAdminReviewsAsync(productId);
    public Task ReplyReviewAsync(int reviewId, string replyText) => _reviewRepository.ReplyProductReviewAsync(reviewId, replyText);
    public Task DeleteReviewAsync(int reviewId) => _reviewRepository.DeleteProductReviewAsync(reviewId);
    public Task<List<PolicyPageViewModel>> GetPolicyPagesAsync() => _policyRepository.GetPolicyPagesAsync();
    public Task SavePolicyPagesAsync(List<PolicyPageViewModel> policies) => _policyRepository.SavePolicyPagesAsync(policies);
    public async Task<AdminWebsitePageViewModel> GetWebsitePageAsync() => new()
    {
        Settings = await _websiteRepository.GetWebsiteSettingsAsync(),
        Slides = await _websiteRepository.GetWebsiteSlidesAsync(),
        AboutPolicies = await _websiteRepository.GetAboutFooterPoliciesAsync()
    };
    public Task SaveWebsitePresentationAsync(AdminWebsitePageViewModel model) => _websiteRepository.SaveWebsitePresentationAsync(model);
    public Task<List<TechNewsPostViewModel>> GetTechNewsPostsAsync(int take = 50) => _techNewsRepository.GetTechNewsPostsAsync(take);
    public Task SaveTechNewsPostsAsync(List<TechNewsPostViewModel> posts) => _techNewsRepository.SaveTechNewsPostsAsync(posts);
    public Task<AdminDashboardViewModel> GetDashboardAsync() => _inventoryRepository.GetAdminDashboardAsync();
}

