namespace websitebanlaptop.Models;

public class AdminWebsitePageViewModel
{
    public WebsiteSettingsViewModel Settings { get; set; } = new();
    public List<WebsiteSlideItemViewModel> Slides { get; set; } = new();
    public List<PolicyPageViewModel> AboutPolicies { get; set; } = new();
}

