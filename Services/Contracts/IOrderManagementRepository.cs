using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IOrderManagementRepository
{
    Task<List<AdminOrderViewModel>> GetAdminOrdersAsync(string status);
    Task MarkOrderDeliveredAsync(int orderId);
    Task CancelOrderAsync(int orderId);
    Task<AdminOrderViewModel?> GetAdminOrderDetailAsync(int orderId);
}

