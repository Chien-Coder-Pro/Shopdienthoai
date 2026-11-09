using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("HOADON")]
    [Index("MaHang", Name = "IX_HOADON_MaHang")]
    [Index("MaNd", Name = "IX_HOADON_MaND")]
    public partial class Hoadon
    {
        [Key]
        [Column("MaHD")]
        public int MaHd
        {
            get; set;
        }

        [Column("MaND")]
        public int MaNd
        {
            get; set;
        }

        [Column(TypeName = "datetime")]
        public DateTime NgayLap
        {
            get; set;
        }

        [StringLength(50)]
        public string NguoiNhan { get; set; } = null!;

        [Column("SDT")]
        [StringLength(20)]
        [Unicode(false)]
        public string Sdt { get; set; } = null!;

        [StringLength(100)]
        public string DiaChi { get; set; } = null!;

        [Column("PhuongThucTT")]
        [StringLength(20)]
        public string PhuongThucTt { get; set; } = null!;

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTien
        {
            get; set;
        }

        public int? MaHang
        {
            get; set;
        }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal PhanTramGiam
        {
            get; set;
        }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TienGiam
        {
            get; set;
        }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal? TongThanhToan
        {
            get; set;
        }

        [StringLength(70)]
        public string TrangThai { get; set; } = null!;

        [InverseProperty("MaHdNavigation")]
        public virtual ICollection<Chitiethoadon> Chitiethoadons { get; set; } = new List<Chitiethoadon>();

        [ForeignKey("MaHang")]
        [InverseProperty("Hoadons")]
        public virtual Hangkhachhang? MaHangNavigation
        {
            get; set;
        }

        [ForeignKey("MaNd")]
        [InverseProperty("Hoadons")]
        public virtual Nguoidung MaNdNavigation { get; set; } = null!;

        // Các thuộc tính bổ sung cho chức năng shop.
        [StringLength(30)]
        public string TrangThaiThanhToan { get; set; } = ShopRules.Unpaid;
        [StringLength(36)]
        public string? MaDatHang
        {
            get; set;
        }
    }
}
