using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminUserRepository
{
    Task<List<AdminUserItemViewModel>> GetAdminUsersAsync();
    Task<AdminUserFormViewModel?> GetAdminUserByIdAsync(int userId);
    Task CreateAdminUserAsync(AdminUserFormViewModel model);
    Task UpdateAdminUserAsync(AdminUserFormViewModel model);
    Task ResetAdminPasswordAsync(int userId, string newPassword);
    Task ToggleAdminUserStatusAsync(int userId);
}

