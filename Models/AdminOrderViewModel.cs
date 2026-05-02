namespace websitebanlaptop.Models;

public class AdminOrderViewModel
{
    public int OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public bool IsPaid { get; set; }
    public decimal TotalAmount { get; set; }
    public string OrderStatus { get; set; } = string.Empty;
    public List<CartItem> Items { get; set; } = new();
    public bool CanMarkDelivered { get; set; }
    public bool CanCancel { get; set; }
}


