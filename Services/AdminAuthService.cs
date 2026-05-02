using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class AdminAuthService : IAdminAuthService
{
    private readonly IAdminAuthRepository _repository;
    public AdminAuthService(IAdminAuthRepository repository) => _repository = repository;
    public Task<AdminUserSessionModel?> ValidateLoginAsync(string username, string password) => _repository.ValidateAdminLoginAsync(username, password);
}

