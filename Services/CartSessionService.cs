using System.Text.Json;
using websitebanlaptop.Models;

namespace websitebanlaptop.Services;

public class CartSessionService
{
    private const string CartKey = "LAPTOPSTORE_CART";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CartSessionService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public List<CartItem> GetCart()
    {
        var json = _httpContextAccessor.HttpContext?.Session.GetString(CartKey);
        return string.IsNullOrWhiteSpace(json)
            ? new List<CartItem>()
            : JsonSerializer.Deserialize<List<CartItem>>(json) ?? new List<CartItem>();
    }

    public void SaveCart(List<CartItem> items)
    {
        _httpContextAccessor.HttpContext?.Session.SetString(CartKey, JsonSerializer.Serialize(items));
    }

    public void AddItem(ProductCardViewModel product, int quantity)
    {
        var items = GetCart();
        var existing = items.FirstOrDefault(x => x.ProductId == product.Id);
        if (existing == null)
        {
            items.Add(new CartItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                Brand = product.Brand,
                ImageUrl = product.PrimaryImage,
                Quantity = quantity,
                UnitPrice = product.Price
            });
        }
        else
        {
            existing.Quantity += quantity;
        }
        SaveCart(items);
    }

    public void UpdateQuantity(int productId, int quantity)
    {
        var items = GetCart();
        var item = items.FirstOrDefault(x => x.ProductId == productId);
        if (item == null) return;
        if (quantity <= 0)
            items.Remove(item);
        else
            item.Quantity = quantity;
        SaveCart(items);
    }

    public void Remove(int productId)
    {
        var items = GetCart();
        var item = items.FirstOrDefault(x => x.ProductId == productId);
        if (item != null)
        {
            items.Remove(item);
            SaveCart(items);
        }
    }

    public void Clear() => SaveCart(new List<CartItem>());
}

