using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("SANPHAM")]
    [Index("MaKm", Name = "IX_SANPHAM_MaKM")]
    [Index("MaLsp", Name = "IX_SANPHAM_MaLSP")]
    public partial class Sanpham
    {
        [Key]
        [Column("MaSP")]
        public int MaSp
        {
            get; set;
        }

        [Column("MaLSP")]
        public int MaLsp
        {
            get; set;
        }

        [Column("TenSP")]
        [StringLength(70)]
        public string TenSp { get; set; } = null!;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal DonGia
        {
            get; set;
        }

        public int SoLuong
        {
            get; set;
        }

        [StringLength(200)]
        [Unicode(false)]
        public string HinhAnh { get; set; } = null!;

        [Column("MaKM")]
        public int MaKm
        {
            get; set;
        }

        [StringLength(50)]
        public string ManHinh { get; set; } = null!;

        [Column("HDH")]
        [StringLength(50)]
        public string Hdh { get; set; } = null!;

        [StringLength(50)]
        public string CamSau { get; set; } = null!;

        [StringLength(50)]
        public string CamTruoc { get; set; } = null!;

        [Column("CPU")]
        [StringLength(50)]
        public string Cpu { get; set; } = null!;

        [StringLength(50)]
        public string Ram { get; set; } = null!;

        [StringLength(50)]
        public string Rom { get; set; } = null!;

        [Column("SDCard")]
        [StringLength(50)]
        public string Sdcard { get; set; } = null!;

        [StringLength(50)]
        public string Pin { get; set; } = null!;

        public int SoSao
        {
            get; set;
        }

        public int SoDanhGia
        {
            get; set;
        }

        public int TrangThai
        {
            get; set;
        }

        [InverseProperty("MaSpNavigation")]
        public virtual ICollection<Chitiethoadon> Chitiethoadons { get; set; } = new List<Chitiethoadon>();

        [InverseProperty("MaSpNavigation")]
        public virtual ICollection<Danhgium> Danhgia { get; set; } = new List<Danhgium>();

        [InverseProperty("MaSpNavigation")]
        public virtual ICollection<Giohang> Giohangs { get; set; } = new List<Giohang>();

        [ForeignKey("MaKm")]
        [InverseProperty("Sanphams")]
        public virtual Khuyenmai MaKmNavigation { get; set; } = null!;

        [ForeignKey("MaLsp")]
        [InverseProperty("Sanphams")]
        public virtual Loaisanpham MaLspNavigation { get; set; } = null!;
    }
}
