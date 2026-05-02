using System.ComponentModel.DataAnnotations;

namespace websitebanlaptop.Models;

public class AdminUserFormViewModel
{
    public int UserId { get; set; }

    [Required]
    [Display(Name = "Tên đăng nhập")]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Họ tên")]
    public string FullName { get; set; } = string.Empty;

    [MinLength(6)]
    [DataType(DataType.Password)]
    [Display(Name = "Mật khẩu")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Quản trị hệ thống")]
    public bool IsSuperAdmin { get; set; }

    [Display(Name = "Xem đơn hàng")]
    public bool CanViewOrders { get; set; }

    [Display(Name = "Cập nhật trạng thái đơn")]
    public bool CanUpdateOrders { get; set; }

    [Display(Name = "Hủy đơn hàng")]
    public bool CanCancelOrders { get; set; }

    [Display(Name = "Xem bình luận")]
    public bool CanViewReviews { get; set; }

    [Display(Name = "Trả lời bình luận")]
    public bool CanReplyReviews { get; set; }

    [Display(Name = "Xóa bình luận")]
    public bool CanDeleteReviews { get; set; }

    [Display(Name = "Thêm / sửa / cập nhật thiết bị")]
    public bool CanManageInventory { get; set; }

    [Display(Name = "Xóa thiết bị")]
    public bool CanDeleteInventory { get; set; }

    [Display(Name = "Import thiết bị / danh mục")]
    public bool CanImportInventory { get; set; }

    [Display(Name = "Chỉnh sửa giao diện website")]
    public bool CanManageWebsite { get; set; }

    public bool RequirePassword => UserId == 0;
}

