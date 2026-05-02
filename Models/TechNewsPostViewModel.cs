namespace websitebanlaptop.Models;

public class TechNewsPostViewModel
{
    public int PostId { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string CategoryName { get; set; } = "Tin công nghệ";
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public string ContentHtml { get; set; } = string.Empty;
    public string SeoTitle { get; set; } = string.Empty;
    public string MetaDescription { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string PublishedAtText { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public bool IsPublished { get; set; } = true;
}

