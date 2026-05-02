using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public InventoryRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<List<InventoryItemViewModel>> GetInventoryAsync(string? keyword = null, string? brand = null, string? category = null) => _storeRepository.GetInventoryAsync(keyword, brand, category);
    public Task<InventoryFormViewModel?> GetInventoryFormByIdAsync(int productId) => _storeRepository.GetInventoryFormByIdAsync(productId);
    public Task<int> CreateProductAsync(InventoryFormViewModel model) => _storeRepository.CreateProductAsync(model);
    public Task UpdateProductAsync(InventoryFormViewModel model) => _storeRepository.UpdateProductAsync(model);
    public Task UpdateStockAsync(int productId, int stockQty) => _storeRepository.UpdateStockAsync(productId, stockQty);
    public Task DeleteProductAsync(int productId) => _storeRepository.DeleteProductAsync(productId);
    public Task<int> ImportProductsAsync(List<InventoryImportRow> rows) => _storeRepository.ImportProductsAsync(rows);
    public Task<(int products, int totalStock, int lowStock, int orders)> GetAdminSummaryAsync() => _storeRepository.GetAdminSummaryAsync();
    public Task<AdminCatalogViewModel> GetAdminCatalogAsync(string? brand, string? category, string? keyword) => _storeRepository.GetAdminCatalogAsync(brand, category, keyword);
    public Task<AdminDashboardViewModel> GetAdminDashboardAsync() => _storeRepository.GetAdminDashboardAsync();
}

