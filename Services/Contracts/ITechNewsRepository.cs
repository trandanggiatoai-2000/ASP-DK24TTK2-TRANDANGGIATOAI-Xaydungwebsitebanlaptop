using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface ITechNewsRepository
{
    Task<List<TechNewsPostViewModel>> GetTechNewsPostsAsync(int take = 50);
    Task<TechNewsPostViewModel?> GetTechNewsPostBySlugAsync(string slug);
    Task SaveTechNewsPostsAsync(List<TechNewsPostViewModel> posts);
}

