using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopdienthoai.Models
{
    [Table("GIAODICHTHANHTOAN")]
    public class Giaodichthanhtoan
    {
        [Key]
        public int MaGd
        {
            get; set;
        }
        public int MaHd
        {
            get; set;
        }
        [StringLength(100)]
        public string OrderId { get; set; } = "";
        [StringLength(100)]
        public string RequestId { get; set; } = "";
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien
        {
            get; set;
        }
        [StringLength(30)]
        public string TrangThai { get; set; } = "Khởi tạo";
        [StringLength(100)]
        public string? TransId
        {
            get; set;
        }
        public int? ResultCode
        {
            get; set;
        }
        public DateTime NgayTao { get; set; } = ShopRules.Now;
        public DateTime? NgayCapNhat
        {
            get; set;
        }
        [StringLength(500)]
        public string? PayUrl
        {
            get; set;
        }
    }
}
