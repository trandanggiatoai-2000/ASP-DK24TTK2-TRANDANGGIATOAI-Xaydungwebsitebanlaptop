using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class PolicyRepository : IPolicyRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public PolicyRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<List<PolicyPageViewModel>> GetPolicyPagesAsync() => _storeRepository.GetPolicyPagesAsync();
    public Task<PolicyPageViewModel?> GetPolicyByKeyAsync(string key) => _storeRepository.GetPolicyByKeyAsync(key);
    public Task SavePolicyPagesAsync(List<PolicyPageViewModel> policies) => _storeRepository.SavePolicyPagesAsync(policies);
}

