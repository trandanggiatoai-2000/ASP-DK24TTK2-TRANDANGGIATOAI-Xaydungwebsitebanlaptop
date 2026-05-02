using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminUserService
{
    Task<List<AdminUserItemViewModel>> GetUsersAsync();
    Task<AdminUserFormViewModel?> GetUserByIdAsync(int userId);
    Task CreateUserAsync(AdminUserFormViewModel model);
    Task UpdateUserAsync(AdminUserFormViewModel model);
    Task ResetPasswordAsync(int userId, string newPassword);
    Task ToggleStatusAsync(int userId);
}

