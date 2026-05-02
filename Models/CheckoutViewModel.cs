using System.ComponentModel.DataAnnotations;

namespace websitebanlaptop.Models;

public class CheckoutViewModel
{
    [Required]
    public string CustomerName { get; set; } = string.Empty;
    [Required]
    public string Phone { get; set; } = string.Empty;
    [Required]
    public string Address { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    [Required]
    public string PaymentMethod { get; set; } = "PayLater";
}

