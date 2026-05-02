namespace websitebanlaptop.Models;

public class ProductCardViewModel
{
    public int Id { get; set; }
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Cpu { get; set; } = string.Empty;
    public string Ram { get; set; } = string.Empty;
    public string Ssd { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal OldPrice { get; set; }
    public int Stock { get; set; }
    public string BadgeText { get; set; } = string.Empty;

    public bool IsOfficial { get; set; }
    public bool IsFastDelivery { get; set; }
    public bool IsInstallment { get; set; }
    public string OfficialText { get; set; } = "Hàng chính hãng";
    public string FastDeliveryText { get; set; } = "Giao nhanh nội thành";
    public string InstallmentText { get; set; } = "Trả góp 0%";
    public List<string> Images { get; set; } = new();
    public string PrimaryImage => Images.Count > 0 ? Images[0] : "/images/banners/slide2.svg";
}

