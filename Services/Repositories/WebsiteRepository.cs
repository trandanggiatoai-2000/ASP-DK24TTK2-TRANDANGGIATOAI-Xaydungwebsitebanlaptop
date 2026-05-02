using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class WebsiteRepository : IWebsiteRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public WebsiteRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<WebsiteSettingsViewModel> GetWebsiteSettingsAsync() => _storeRepository.GetWebsiteSettingsAsync();
    public Task<List<WebsiteSlideItemViewModel>> GetWebsiteSlidesAsync() => _storeRepository.GetWebsiteSlidesAsync();
    public Task SaveWebsitePresentationAsync(AdminWebsitePageViewModel model) => _storeRepository.SaveWebsitePresentationAsync(model);
    public Task<List<string>> GetTopBrandsForMenuAsync(int top = 8) => _storeRepository.GetTopBrandsForMenuAsync(top);
    public Task<List<string>> GetTopCategoriesForMenuAsync(int top = 8) => _storeRepository.GetTopCategoriesForMenuAsync(top);
    public Task<List<PolicyPageViewModel>> GetAboutFooterPoliciesAsync() => _storeRepository.GetAboutFooterPoliciesAsync();
}

