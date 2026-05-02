using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IInfrastructureRepository
{
    Task<bool> IsDatabaseInitializedAsync();
    Task BootstrapDatabaseAsync();
    Task EnsureAdminSecuritySchemaAsync();
    Task SyncBrandProductImagesAsync();
}

