using System.ComponentModel.DataAnnotations;

namespace Shopdienthoai.Models
{
    public class ProfileForm
    {
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(20)]
        public string Ho { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(20)]
        public string Ten { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0.")]
        public string Sdt { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(200)]
        public string DiaChi { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), RegularExpression("^(Nam|Nữ|Khác)$", ErrorMessage = "Giới tính chưa hợp lệ.")]
        public string GioiTinh { get; set; } = "Khác";
    }
}
