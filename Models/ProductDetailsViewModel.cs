namespace websitebanlaptop.Models;

public class ProductDetailsViewModel : ProductCardViewModel
{
    public string Description { get; set; } = string.Empty;
    public List<string> Features { get; set; } = new();
    public List<ProductReviewViewModel> Reviews { get; set; } = new();
    public double AverageRating { get; set; }
    public int TotalReviews { get; set; }
    public string ReviewerName { get; set; } = string.Empty;
    public int NewRating { get; set; } = 5;
    public string NewComment { get; set; } = string.Empty;
    public int? FilterStar { get; set; }
    public Dictionary<int, int> RatingSummary { get; set; } = new();
}

