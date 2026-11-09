using System.ComponentModel.DataAnnotations;

namespace Shopdienthoai.Models
{
    public class ChangePasswordForm
    {
        [DataType(DataType.Password)]
        public string? MatKhauCu
        {
            get; set;
        }
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự."), DataType(DataType.Password)]
        public string MatKhauMoi { get; set; } = "";
        [Compare(nameof(MatKhauMoi), ErrorMessage = "Mật khẩu xác nhận chưa khớp."), DataType(DataType.Password)]
        public string XacNhan { get; set; } = "";
    }
}
