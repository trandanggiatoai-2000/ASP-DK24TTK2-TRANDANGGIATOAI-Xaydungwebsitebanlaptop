using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminMediaService
{
    Task ApplyInventoryImagesAsync(InventoryFormViewModel model);
    Task ApplyWebsiteAssetsAsync(AdminWebsitePageViewModel model);
}

