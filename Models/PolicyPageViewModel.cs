namespace websitebanlaptop.Models;

public class PolicyPageViewModel
{
    public int PolicyId { get; set; }
    public string PolicyKey { get; set; } = string.Empty;
    public string MenuLabel { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}

