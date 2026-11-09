using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("KHUYENMAI")]
    public partial class Khuyenmai
    {
        [Key]
        [Column("MaKM")]
        public int MaKm
        {
            get; set;
        }

        [Column("TenKM")]
        [StringLength(100)]
        public string TenKm { get; set; } = null!;

        [Column("LoaiKM")]
        [StringLength(20)]
        public string LoaiKm { get; set; } = null!;

        [Column("GiaTriKM", TypeName = "decimal(18, 2)")]
        public decimal GiaTriKm
        {
            get; set;
        }

        [Column("NgayBD", TypeName = "datetime")]
        public DateTime NgayBd
        {
            get; set;
        }

        [Column("NgayKT", TypeName = "datetime")]
        public DateTime NgayKt
        {
            get; set;
        }

        [InverseProperty("MaKmNavigation")]
        public virtual ICollection<Sanpham> Sanphams { get; set; } = new List<Sanpham>();
    }
}
