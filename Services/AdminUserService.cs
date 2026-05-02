using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class AdminUserService : IAdminUserService
{
    private readonly IAdminUserRepository _repository;
    public AdminUserService(IAdminUserRepository repository) => _repository = repository;
    public Task<List<AdminUserItemViewModel>> GetUsersAsync() => _repository.GetAdminUsersAsync();
    public Task<AdminUserFormViewModel?> GetUserByIdAsync(int userId) => _repository.GetAdminUserByIdAsync(userId);
    public Task CreateUserAsync(AdminUserFormViewModel model) => _repository.CreateAdminUserAsync(model);
    public Task UpdateUserAsync(AdminUserFormViewModel model) => _repository.UpdateAdminUserAsync(model);
    public Task ResetPasswordAsync(int userId, string newPassword) => _repository.ResetAdminPasswordAsync(userId, newPassword);
    public Task ToggleStatusAsync(int userId) => _repository.ToggleAdminUserStatusAsync(userId);
}

