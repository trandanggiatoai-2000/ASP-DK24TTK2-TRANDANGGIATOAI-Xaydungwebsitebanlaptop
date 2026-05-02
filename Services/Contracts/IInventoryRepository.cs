using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IInventoryRepository
{
    Task<List<InventoryItemViewModel>> GetInventoryAsync(string? keyword = null, string? brand = null, string? category = null);
    Task<InventoryFormViewModel?> GetInventoryFormByIdAsync(int productId);
    Task<int> CreateProductAsync(InventoryFormViewModel model);
    Task UpdateProductAsync(InventoryFormViewModel model);
    Task UpdateStockAsync(int productId, int stockQty);
    Task DeleteProductAsync(int productId);
    Task<int> ImportProductsAsync(List<InventoryImportRow> rows);
    Task<(int products, int totalStock, int lowStock, int orders)> GetAdminSummaryAsync();
    Task<AdminCatalogViewModel> GetAdminCatalogAsync(string? brand, string? category, string? keyword);
    Task<AdminDashboardViewModel> GetAdminDashboardAsync();
}

