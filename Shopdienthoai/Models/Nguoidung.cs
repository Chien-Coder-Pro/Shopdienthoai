using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("NGUOIDUNG")]
    [Index("MaHang", Name = "IX_NGUOIDUNG_MaHang")]
    [Index("MaQuyen", Name = "IX_NGUOIDUNG_MaQuyen")]
    public partial class Nguoidung
    {
        [Key]
        [Column("MaND")]
        public int MaNd
        {
            get; set;
        }

        [StringLength(20)]
        public string Ho { get; set; } = null!;

        [StringLength(20)]
        public string Ten { get; set; } = null!;

        [StringLength(10)]
        public string GioiTinh { get; set; } = null!;

        [Column("SDT")]
        [StringLength(20)]
        [Unicode(false)]
        public string Sdt { get; set; } = null!;

        [StringLength(50)]
        [Unicode(false)]
        public string Email { get; set; } = null!;

        [StringLength(200)]
        public string DiaChi { get; set; } = null!;

        [StringLength(100)]
        public string TaiKhoan { get; set; } = null!;

        [StringLength(255)]
        [Unicode(false)]
        public string MatKhau { get; set; } = null!;

        public int MaQuyen
        {
            get; set;
        }

        public int MaHang
        {
            get; set;
        }

        public int TrangThai
        {
            get; set;
        }

        [InverseProperty("MaNdNavigation")]
        public virtual ICollection<Danhgium> Danhgia { get; set; } = new List<Danhgium>();

        [InverseProperty("MaNdNavigation")]
        public virtual ICollection<Giohang> Giohangs { get; set; } = new List<Giohang>();

        [InverseProperty("MaNdNavigation")]
        public virtual ICollection<Hoadon> Hoadons { get; set; } = new List<Hoadon>();

        [ForeignKey("MaHang")]
        [InverseProperty("Nguoidungs")]
        public virtual Hangkhachhang MaHangNavigation { get; set; } = null!;

        [ForeignKey("MaQuyen")]
        [InverseProperty("Nguoidungs")]
        public virtual Phanquyen MaQuyenNavigation { get; set; } = null!;

        // Các thuộc tính bổ sung cho chức năng shop.
        [StringLength(100)]
        public string? GoogleId
        {
            get; set;
        }
        public bool CoMatKhau { get; set; } = true;
    }
}
