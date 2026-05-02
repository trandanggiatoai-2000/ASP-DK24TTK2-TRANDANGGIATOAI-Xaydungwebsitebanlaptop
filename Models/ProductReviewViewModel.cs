namespace websitebanlaptop.Models;

public class ProductReviewViewModel
{
    public int ReviewId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ReviewerName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string CommentText { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string ReplyText { get; set; } = string.Empty;
    public DateTime? ReplyCreatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

