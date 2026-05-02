using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class InfrastructureRepository : IInfrastructureRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public InfrastructureRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<bool> IsDatabaseInitializedAsync() => _storeRepository.IsDatabaseInitializedAsync();
    public Task BootstrapDatabaseAsync() => _storeRepository.BootstrapDatabaseAsync();
    public Task EnsureAdminSecuritySchemaAsync() => _storeRepository.EnsureAdminSecuritySchemaAsync();
    public Task SyncBrandProductImagesAsync() => _storeRepository.SyncBrandProductImagesAsync();
}

