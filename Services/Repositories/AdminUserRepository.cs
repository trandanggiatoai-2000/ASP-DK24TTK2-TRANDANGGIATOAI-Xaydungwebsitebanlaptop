using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class AdminUserRepository : IAdminUserRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public AdminUserRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<List<AdminUserItemViewModel>> GetAdminUsersAsync() => _storeRepository.GetAdminUsersAsync();
    public Task<AdminUserFormViewModel?> GetAdminUserByIdAsync(int userId) => _storeRepository.GetAdminUserByIdAsync(userId);
    public Task CreateAdminUserAsync(AdminUserFormViewModel model) => _storeRepository.CreateAdminUserAsync(model);
    public Task UpdateAdminUserAsync(AdminUserFormViewModel model) => _storeRepository.UpdateAdminUserAsync(model);
    public Task ResetAdminPasswordAsync(int userId, string newPassword) => _storeRepository.ResetAdminPasswordAsync(userId, newPassword);
    public Task ToggleAdminUserStatusAsync(int userId) => _storeRepository.ToggleAdminUserStatusAsync(userId);
}

