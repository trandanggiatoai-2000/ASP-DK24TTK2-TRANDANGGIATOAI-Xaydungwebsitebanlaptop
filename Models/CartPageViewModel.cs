namespace websitebanlaptop.Models;

public class CartPageViewModel
{
    public List<CartItem> Items { get; set; } = new();
    public decimal Subtotal => Items.Sum(x => x.LineTotal);
    public decimal ShippingFee => Subtotal >= 15000000 ? 0 : 50000;
    public decimal Total => Subtotal + ShippingFee;
}

