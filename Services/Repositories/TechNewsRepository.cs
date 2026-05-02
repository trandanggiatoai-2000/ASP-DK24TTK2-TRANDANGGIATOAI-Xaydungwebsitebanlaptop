using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class TechNewsRepository : ITechNewsRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public TechNewsRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<List<TechNewsPostViewModel>> GetTechNewsPostsAsync(int take = 50) => _storeRepository.GetTechNewsPostsAsync(take);
    public Task<TechNewsPostViewModel?> GetTechNewsPostBySlugAsync(string slug) => _storeRepository.GetTechNewsPostBySlugAsync(slug);
    public Task SaveTechNewsPostsAsync(List<TechNewsPostViewModel> posts) => _storeRepository.SaveTechNewsPostsAsync(posts);
}

