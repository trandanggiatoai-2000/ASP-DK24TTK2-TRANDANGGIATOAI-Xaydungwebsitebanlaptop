using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminContentService
{
    Task<AdminCatalogViewModel> GetCatalogAsync(string? brand, string? category, string? keyword);
    Task<List<AdminReviewItemViewModel>> GetReviewsAsync(int? productId = null);
    Task ReplyReviewAsync(int reviewId, string replyText);
    Task DeleteReviewAsync(int reviewId);
    Task<List<PolicyPageViewModel>> GetPolicyPagesAsync();
    Task SavePolicyPagesAsync(List<PolicyPageViewModel> policies);
    Task<AdminWebsitePageViewModel> GetWebsitePageAsync();
    Task SaveWebsitePresentationAsync(AdminWebsitePageViewModel model);
    Task<List<TechNewsPostViewModel>> GetTechNewsPostsAsync(int take = 50);
    Task SaveTechNewsPostsAsync(List<TechNewsPostViewModel> posts);
    Task<AdminDashboardViewModel> GetDashboardAsync();
}

