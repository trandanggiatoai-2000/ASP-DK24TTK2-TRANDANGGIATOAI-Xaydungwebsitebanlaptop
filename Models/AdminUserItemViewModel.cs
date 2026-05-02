namespace websitebanlaptop.Models;

public class AdminUserItemViewModel
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsSuperAdmin { get; set; }
    public bool CanViewOrders { get; set; }
    public bool CanUpdateOrders { get; set; }
    public bool CanCancelOrders { get; set; }
    public bool CanViewReviews { get; set; }
    public bool CanReplyReviews { get; set; }
    public bool CanDeleteReviews { get; set; }
    public bool CanManageInventory { get; set; }
    public bool CanDeleteInventory { get; set; }
    public bool CanImportInventory { get; set; }
    public bool CanManageWebsite { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

