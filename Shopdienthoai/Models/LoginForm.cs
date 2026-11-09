using System.ComponentModel.DataAnnotations;

namespace Shopdienthoai.Models
{
    public class LoginForm
    {
        [Required(ErrorMessage = "Nhập tài khoản hoặc email.")]
        public string TaiKhoan { get; set; } = "";
        [Required(ErrorMessage = "Nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";
        public string? ReturnUrl
        {
            get; set;
        }
    }
}
