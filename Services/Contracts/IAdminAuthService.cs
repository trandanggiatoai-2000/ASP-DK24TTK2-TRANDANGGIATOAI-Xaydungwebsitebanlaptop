using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminAuthService
{
    Task<AdminUserSessionModel?> ValidateLoginAsync(string username, string password);
}

