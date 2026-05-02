using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class LayoutContentService : ILayoutContentService
{
    private readonly IWebsiteRepository _websiteRepository;

    public LayoutContentService(IWebsiteRepository websiteRepository)
    {
        _websiteRepository = websiteRepository;
    }

    public Task<WebsiteSettingsViewModel> GetWebsiteSettingsAsync() => _websiteRepository.GetWebsiteSettingsAsync();
    public Task<List<string>> GetTopBrandsForMenuAsync(int top = 8) => _websiteRepository.GetTopBrandsForMenuAsync(top);
    public Task<List<string>> GetTopCategoriesForMenuAsync(int top = 8) => _websiteRepository.GetTopCategoriesForMenuAsync(top);
    public Task<List<PolicyPageViewModel>> GetAboutFooterPoliciesAsync() => _websiteRepository.GetAboutFooterPoliciesAsync();
}

