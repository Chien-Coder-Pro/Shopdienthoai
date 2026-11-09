using System.ComponentModel.DataAnnotations;

namespace Shopdienthoai.Models
{
    public class CheckoutForm
    {
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(36)]
        public string Token { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(50)]
        public string NguoiNhan { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), RegularExpression(@"^0[0-9]{9}$", ErrorMessage = "Số điện thoại gồm 10 số, bắt đầu bằng 0.")]
        public string Sdt { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), StringLength(100)]
        public string DiaChi { get; set; } = "";
        [Required(ErrorMessage = "Vui lòng nhập thông tin này."), RegularExpression("^(Trực tiếp|MoMo)$")]
        public string PhuongThucTt { get; set; } = "Trực tiếp";
    }
}
