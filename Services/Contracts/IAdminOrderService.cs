using websitebanlaptop.Models;

namespace websitebanlaptop.Services.Contracts;

public interface IAdminOrderService
{
    Task<List<AdminOrderViewModel>> GetOrdersAsync(string status);
    Task<AdminOrderViewModel?> GetOrderDetailAsync(int orderId);
    Task MarkDeliveredAsync(int orderId);
    Task CancelAsync(int orderId);
}

