using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [PrimaryKey("MaNd", "MaSp")]
    [Table("GIOHANG")]
    public partial class Giohang
    {
        [Key]
        [Column("MaND")]
        public int MaNd
        {
            get; set;
        }

        [Key]
        [Column("MaSP")]
        public int MaSp
        {
            get; set;
        }

        public int SoLuong
        {
            get; set;
        }

        [ForeignKey("MaNd")]
        [InverseProperty("Giohangs")]
        public virtual Nguoidung MaNdNavigation { get; set; } = null!;

        [ForeignKey("MaSp")]
        [InverseProperty("Giohangs")]
        public virtual Sanpham MaSpNavigation { get; set; } = null!;
    }
}
