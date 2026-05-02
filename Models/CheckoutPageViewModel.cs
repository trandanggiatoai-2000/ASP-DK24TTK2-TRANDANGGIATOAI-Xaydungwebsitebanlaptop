namespace websitebanlaptop.Models;

public class CheckoutPageViewModel
{
    public CheckoutViewModel Form { get; set; } = new();
    public CartPageViewModel Cart { get; set; } = new();
    public string BankAccountName { get; set; } = "LAPTOP STORE PREMIUM";
    public string BankNumber { get; set; } = "190012340001";
    public string BankName { get; set; } = "VCB - Vietcombank";
}

