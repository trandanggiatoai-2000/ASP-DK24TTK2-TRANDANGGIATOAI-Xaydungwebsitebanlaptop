using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class ReviewManagementRepository : IReviewManagementRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public ReviewManagementRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<List<AdminReviewItemViewModel>> GetAdminReviewsAsync(int? productId = null) => _storeRepository.GetAdminReviewsAsync(productId);
    public Task ReplyProductReviewAsync(int reviewId, string replyText) => _storeRepository.ReplyProductReviewAsync(reviewId, replyText);
    public Task DeleteProductReviewAsync(int reviewId) => _storeRepository.DeleteProductReviewAsync(reviewId);
}

