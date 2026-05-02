using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class AdminAuthRepository : IAdminAuthRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public AdminAuthRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<AdminUserSessionModel?> ValidateAdminLoginAsync(string username, string password) => _storeRepository.ValidateAdminLoginAsync(username, password);
}

