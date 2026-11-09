using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("CHITIETHOADON")]
    [Index("MaHd", Name = "IX_CHITIETHOADON_MaHD")]
    [Index("MaSp", Name = "IX_CHITIETHOADON_MaSP")]
    public partial class Chitiethoadon
    {
        [Key]
        [Column("MaCTHD")]
        public int MaCthd
        {
            get; set;
        }

        [Column("MaHD")]
        public int MaHd
        {
            get; set;
        }

        [Column("MaSP")]
        public int MaSp
        {
            get; set;
        }

        public int SoLuong
        {
            get; set;
        }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DonGia
        {
            get; set;
        }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ThanhTien
        {
            get; set;
        }

        [ForeignKey("MaHd")]
        [InverseProperty("Chitiethoadons")]
        public virtual Hoadon MaHdNavigation { get; set; } = null!;

        [ForeignKey("MaSp")]
        [InverseProperty("Chitiethoadons")]
        public virtual Sanpham MaSpNavigation { get; set; } = null!;
    }
}
