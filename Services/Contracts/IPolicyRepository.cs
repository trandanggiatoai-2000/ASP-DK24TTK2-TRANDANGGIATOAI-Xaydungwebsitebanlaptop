using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IPolicyRepository
{
    Task<List<PolicyPageViewModel>> GetPolicyPagesAsync();
    Task<PolicyPageViewModel?> GetPolicyByKeyAsync(string key);
    Task SavePolicyPagesAsync(List<PolicyPageViewModel> policies);
}

