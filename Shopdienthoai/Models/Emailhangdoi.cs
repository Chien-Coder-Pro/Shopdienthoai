using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopdienthoai.Models
{
    [Table("EMAILHANGDOI")]
    public class Emailhangdoi
    {
        [Key]
        public int MaEmail
        {
            get; set;
        }
        [StringLength(100)]
        public string Khoa { get; set; } = "";
        [StringLength(100)]
        public string NguoiNhan { get; set; } = "";
        [StringLength(200)]
        public string TieuDe { get; set; } = "";
        public string NoiDung { get; set; } = "";
        public int SoLanThu
        {
            get; set;
        }
        public DateTime NgayTao { get; set; } = ShopRules.Now;
        public DateTime? DaGuiLuc
        {
            get; set;
        }
        public DateTime? ThuTiepLuc
        {
            get; set;
        }
        [StringLength(200)]
        public string? LoiGanNhat
        {
            get; set;
        }
    }
}
