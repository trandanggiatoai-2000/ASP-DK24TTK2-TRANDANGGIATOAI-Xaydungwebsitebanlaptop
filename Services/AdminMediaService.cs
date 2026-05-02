using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class AdminMediaService : IAdminMediaService
{
    private readonly IFileStorageService _fileStorageService;

    public AdminMediaService(IFileStorageService fileStorageService)
    {
        _fileStorageService = fileStorageService;
    }

    public async Task ApplyInventoryImagesAsync(InventoryFormViewModel model)
    {
        var images = new List<string>();
        if (!string.IsNullOrWhiteSpace(model.ImageGalleryText))
        {
            images.AddRange(model.ImageGalleryText
                .Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(NormalizeMediaUrl)
                .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        if (model.NewImageFiles != null)
        {
            foreach (var file in model.NewImageFiles)
            {
                var uploaded = await _fileStorageService.SaveAsync(file, "products");
                if (!string.IsNullOrWhiteSpace(uploaded))
                {
                    images.Add(uploaded);
                }
            }
        }

        model.ExistingImages = images
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(NormalizeMediaUrl)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        model.ImageGalleryText = string.Join(Environment.NewLine, model.ExistingImages);
    }

    public async Task ApplyWebsiteAssetsAsync(AdminWebsitePageViewModel model)
    {
        model.Slides ??= new List<WebsiteSlideItemViewModel>();
        model.AboutPolicies ??= new List<PolicyPageViewModel>();

        if (model.Settings != null)
        {
            var websiteLogoUrl = await _fileStorageService.SaveAsync(model.Settings.WebsiteLogoFile, "branding");
            if (!string.IsNullOrWhiteSpace(websiteLogoUrl))
            {
                model.Settings.WebsiteLogoUrl = websiteLogoUrl;
            }

            var adminLogoUrl = await _fileStorageService.SaveAsync(model.Settings.AdminLogoFile, "branding");
            if (!string.IsNullOrWhiteSpace(adminLogoUrl))
            {
                model.Settings.AdminLogoUrl = adminLogoUrl;
            }
            else if (!string.IsNullOrWhiteSpace(websiteLogoUrl))
            {
                model.Settings.AdminLogoUrl = websiteLogoUrl;
            }
            else if (string.IsNullOrWhiteSpace(model.Settings.AdminLogoUrl) && !string.IsNullOrWhiteSpace(model.Settings.WebsiteLogoUrl))
            {
                model.Settings.AdminLogoUrl = model.Settings.WebsiteLogoUrl;
            }

            await SaveIfUploadedAsync(model.Settings.FaviconFile, "branding", value => model.Settings.FaviconUrl = value);
            await SaveIfUploadedAsync(model.Settings.FlashImageFile, "banners", value => model.Settings.FlashImageUrl = value);
            await SaveIfUploadedAsync(model.Settings.PopupImageFile, "banners", value => model.Settings.PopupImageUrl = value);
            await SaveIfUploadedAsync(model.Settings.CategoryBannerGamingFile, "banners", value => model.Settings.CategoryBannerGamingUrl = value);
            await SaveIfUploadedAsync(model.Settings.CategoryBannerOfficeFile, "banners", value => model.Settings.CategoryBannerOfficeUrl = value);
            await SaveIfUploadedAsync(model.Settings.CategoryBannerPremiumFile, "banners", value => model.Settings.CategoryBannerPremiumUrl = value);
            await SaveIfUploadedAsync(model.Settings.CategoryBannerGraphicsFile, "banners", value => model.Settings.CategoryBannerGraphicsUrl = value);
            await SaveIfUploadedAsync(model.Settings.CategoryBannerAccessoryFile, "banners", value => model.Settings.CategoryBannerAccessoryUrl = value);
            await SaveIfUploadedAsync(model.Settings.CategoryBannerComponentFile, "banners", value => model.Settings.CategoryBannerComponentUrl = value);

            await SaveIfUploadedAsync(model.Settings.FooterShippingImage1File, "branding", value => model.Settings.FooterShippingImage1Url = value, 120, 40);
            await SaveIfUploadedAsync(model.Settings.FooterShippingImage2File, "branding", value => model.Settings.FooterShippingImage2Url = value, 120, 40);
            await SaveIfUploadedAsync(model.Settings.FooterShippingImage3File, "branding", value => model.Settings.FooterShippingImage3Url = value, 120, 40);
            await SaveIfUploadedAsync(model.Settings.FooterShippingImage4File, "branding", value => model.Settings.FooterShippingImage4Url = value, 120, 40);
            await SaveIfUploadedAsync(model.Settings.FooterQrImageFile, "branding", value => model.Settings.FooterQrImageUrl = value, 180, 180);
        }

        var displayOrder = 1;
        foreach (var slide in model.Slides.OrderBy(x => x.DisplayOrder).ThenBy(x => x.SlideId))
        {
            var uploaded = await _fileStorageService.SaveAsync(slide.UploadFile, "banners");
            if (!string.IsNullOrWhiteSpace(uploaded))
            {
                slide.ImageUrl = uploaded;
            }

            slide.DisplayOrder = displayOrder++;
        }
    }

    private static string NormalizeMediaUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return string.Empty;
        }

        var value = url.Trim();
        var queryIndex = value.IndexOf('?');
        if (queryIndex >= 0)
        {
            value = value[..queryIndex];
        }

        return value.Trim();
    }

    private async Task SaveIfUploadedAsync(IFormFile? file, string folder, Action<string> assign, int? maxWidth = null, int? maxHeight = null)
    {
        var url = await _fileStorageService.SaveAsync(file, folder, maxWidth, maxHeight);
        if (!string.IsNullOrWhiteSpace(url))
        {
            assign(url);
        }
    }
}

