using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminInventoryService
{
    Task<(List<InventoryItemViewModel> Items, List<string> Brands, List<string> Categories)> GetInventoryPageDataAsync(string? keyword, string? brand, string? category);
    Task<(int products, int totalStock, int lowStock, int orders)> GetSummaryAsync();
    Task<InventoryFormViewModel?> GetInventoryFormByIdAsync(int productId);
    Task<int> CreateProductAsync(InventoryFormViewModel model);
    Task UpdateProductAsync(InventoryFormViewModel model);
    Task UpdateStockAsync(int productId, int stockQty);
    Task DeleteProductAsync(int productId);
    Task<int> ImportProductsAsync(List<InventoryImportRow> rows);
}

