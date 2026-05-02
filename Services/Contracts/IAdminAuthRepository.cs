using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminAuthRepository
{
    Task<AdminUserSessionModel?> ValidateAdminLoginAsync(string username, string password);
}

