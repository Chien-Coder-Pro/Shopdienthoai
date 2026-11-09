using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("HANGKHACHHANG")]
    [Index("TenHang", Name = "UQ__HANGKHAC__E6C9BFA8D2ECFEEC", IsUnique = true)]
    public partial class Hangkhachhang
    {
        [Key]
        public int MaHang
        {
            get; set;
        }

        [StringLength(20)]
        public string TenHang { get; set; } = null!;

        [Column(TypeName = "decimal(5, 2)")]
        public decimal PhanTramGiam
        {
            get; set;
        }

        [StringLength(255)]
        public string? MoTa
        {
            get; set;
        }

        [InverseProperty("MaHangNavigation")]
        public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

        [InverseProperty("MaHangNavigation")]
        public virtual ICollection<Nguoidung> Nguoidungs { get; set; } = new List<Nguoidung>();

        // Các thuộc tính bổ sung cho chức năng shop.
        [Column(TypeName = "decimal(18,2)")]
        public decimal NguongChiTieu
        {
            get; set;
        }
    }
}
