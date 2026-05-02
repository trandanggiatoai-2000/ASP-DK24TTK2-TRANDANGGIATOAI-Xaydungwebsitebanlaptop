using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class AdminInventoryService : IAdminInventoryService
{
    private readonly IInventoryRepository _repository;
    public AdminInventoryService(IInventoryRepository repository) => _repository = repository;

    public async Task<(List<InventoryItemViewModel> Items, List<string> Brands, List<string> Categories)> GetInventoryPageDataAsync(string? keyword, string? brand, string? category)
    {
        var allInventory = await _repository.GetInventoryAsync();
        var brands = allInventory.Select(x => x.Brand).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList()!;
        var categories = allInventory.Select(x => x.CategoryName).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList()!;
        var items = await _repository.GetInventoryAsync(keyword, brand, category);
        return (items, brands!, categories!);
    }

    public Task<(int products, int totalStock, int lowStock, int orders)> GetSummaryAsync() => _repository.GetAdminSummaryAsync();
    public Task<InventoryFormViewModel?> GetInventoryFormByIdAsync(int productId) => _repository.GetInventoryFormByIdAsync(productId);
    public Task<int> CreateProductAsync(InventoryFormViewModel model) => _repository.CreateProductAsync(model);
    public Task UpdateProductAsync(InventoryFormViewModel model) => _repository.UpdateProductAsync(model);
    public Task UpdateStockAsync(int productId, int stockQty) => _repository.UpdateStockAsync(productId, stockQty);
    public Task DeleteProductAsync(int productId) => _repository.DeleteProductAsync(productId);
    public Task<int> ImportProductsAsync(List<InventoryImportRow> rows) => _repository.ImportProductsAsync(rows);
}

