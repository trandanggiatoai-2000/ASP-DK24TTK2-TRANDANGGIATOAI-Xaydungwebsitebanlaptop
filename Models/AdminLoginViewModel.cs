using System.ComponentModel.DataAnnotations;

namespace websitebanlaptop.Models;

public class AdminLoginViewModel
{
    [Required(ErrorMessage = "Vui lòng nhập tài khoản admin")]
    public string Username { get; set; } = "admin";

    [Required(ErrorMessage = "Vui lòng nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;
}

