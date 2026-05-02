using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IReviewManagementRepository
{
    Task<List<AdminReviewItemViewModel>> GetAdminReviewsAsync(int? productId = null);
    Task ReplyProductReviewAsync(int reviewId, string replyText);
    Task DeleteProductReviewAsync(int reviewId);
}

