using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services.Repositories;

public class OrderManagementRepository : IOrderManagementRepository
{
    private readonly LaptopStoreRepository _storeRepository;
    public OrderManagementRepository(LaptopStoreRepository storeRepository) => _storeRepository = storeRepository;
    public Task<List<AdminOrderViewModel>> GetAdminOrdersAsync(string status) => _storeRepository.GetAdminOrdersAsync(status);
    public Task MarkOrderDeliveredAsync(int orderId) => _storeRepository.MarkOrderDeliveredAsync(orderId);
    public Task CancelOrderAsync(int orderId) => _storeRepository.CancelOrderAsync(orderId);
    public Task<AdminOrderViewModel?> GetAdminOrderDetailAsync(int orderId) => _storeRepository.GetAdminOrderDetailAsync(orderId);
}

