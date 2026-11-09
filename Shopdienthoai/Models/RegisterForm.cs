using System.ComponentModel.DataAnnotations;

namespace Shopdienthoai.Models
{
    public class RegisterForm
    {
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(20)]
        public string Ho { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(20)]
        public string Ten { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(100, MinimumLength = 4, ErrorMessage = "Tên tài khoản phải từ 4 đến 100 ký tự."), RegularExpression(@"^[a-zA-Z0-9_.-]+$", ErrorMessage = "Tài khoản chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
        public string TaiKhoan { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), EmailAddress(ErrorMessage = "Email chưa đúng định dạng."), StringLength(50)]
        public string Email { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0.")]
        public string Sdt { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(200)]
        public string DiaChi { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), RegularExpression("^(Nam|Nữ|Khác)$", ErrorMessage = "Giới tính chưa hợp lệ.")]
        public string GioiTinh { get; set; } = "Khác";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(100, MinimumLength = 8, ErrorMessage = "Mật khẩu phải từ 8 đến 100 ký tự."), DataType(DataType.Password)]
        public string MatKhau { get; set; } = "";
        [Compare(nameof(MatKhau), ErrorMessage = "Mật khẩu xác nhận chưa khớp."), DataType(DataType.Password)]
        public string XacNhanMatKhau { get; set; } = "";
    }
}
