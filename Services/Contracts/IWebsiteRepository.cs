using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IWebsiteRepository
{
    Task<WebsiteSettingsViewModel> GetWebsiteSettingsAsync();
    Task<List<WebsiteSlideItemViewModel>> GetWebsiteSlidesAsync();
    Task SaveWebsitePresentationAsync(AdminWebsitePageViewModel model);
    Task<List<string>> GetTopBrandsForMenuAsync(int top = 8);
    Task<List<string>> GetTopCategoriesForMenuAsync(int top = 8);
    Task<List<PolicyPageViewModel>> GetAboutFooterPoliciesAsync();
}

