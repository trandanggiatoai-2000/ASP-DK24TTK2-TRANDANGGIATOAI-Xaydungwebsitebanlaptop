namespace websitebanlaptop.Models;

public class AdminDashboardViewModel
{
    public int TotalProducts { get; set; }
    public int TotalStock { get; set; }
    public int LowStockProducts { get; set; }
    public int TotalOrders { get; set; }
    public int PaidOrders { get; set; }
    public int UnpaidOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public int CancelledOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal AverageOrderValue { get; set; }
    public decimal InventoryValue { get; set; }
    public List<DashboardTrendPoint> RevenueByDay { get; set; } = new();
    public List<DashboardBreakdownItem> OrdersByStatus { get; set; } = new();
    public List<DashboardBreakdownItem> StockByCategory { get; set; } = new();
    public List<DashboardBreakdownItem> ProductsByBrand { get; set; } = new();
    public List<DashboardRecentOrderItem> RecentOrders { get; set; } = new();
    public List<DashboardLowStockItem> LowStockItems { get; set; } = new();
}

public class DashboardTrendPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
}

public class DashboardBreakdownItem
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public string DisplayValue { get; set; } = string.Empty;
}

public class DashboardRecentOrderItem
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
}

public class DashboardLowStockItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int StockQty { get; set; }
}

