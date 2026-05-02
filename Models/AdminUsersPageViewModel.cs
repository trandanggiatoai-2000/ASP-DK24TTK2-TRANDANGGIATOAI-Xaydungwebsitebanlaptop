namespace websitebanlaptop.Models;

public class AdminUsersPageViewModel
{
    public List<AdminUserItemViewModel> Users { get; set; } = new();
    public AdminUserFormViewModel Form { get; set; } = new();
}

