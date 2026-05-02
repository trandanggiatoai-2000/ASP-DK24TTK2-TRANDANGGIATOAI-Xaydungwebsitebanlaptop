using websitebanlaptop.Models;
using websitebanlaptop.Services.Contracts;

namespace websitebanlaptop.Services;

public class AdminOrderService : IAdminOrderService
{
    private readonly IOrderManagementRepository _repository;
    public AdminOrderService(IOrderManagementRepository repository) => _repository = repository;
    public Task<List<AdminOrderViewModel>> GetOrdersAsync(string status) => _repository.GetAdminOrdersAsync(status);
    public Task<AdminOrderViewModel?> GetOrderDetailAsync(int orderId) => _repository.GetAdminOrderDetailAsync(orderId);
    public Task MarkDeliveredAsync(int orderId) => _repository.MarkOrderDeliveredAsync(orderId);
    public Task CancelAsync(int orderId) => _repository.CancelOrderAsync(orderId);
}

