using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface ILayoutContentService
{
    Task<WebsiteSettingsViewModel> GetWebsiteSettingsAsync();
    Task<List<string>> GetTopBrandsForMenuAsync(int top = 8);
    Task<List<string>> GetTopCategoriesForMenuAsync(int top = 8);
    Task<List<PolicyPageViewModel>> GetAboutFooterPoliciesAsync();
}

