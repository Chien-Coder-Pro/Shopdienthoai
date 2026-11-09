using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("DANHGIA")]
    [Index("MaSp", Name = "IX_DANHGIA_MaSP")]
    public partial class Danhgium
    {
        [Key]
        [Column("MaDG")]
        public int MaDg
        {
            get; set;
        }

        [Column("MaSP")]
        public int MaSp
        {
            get; set;
        }

        [Column("MaND")]
        public int MaNd
        {
            get; set;
        }

        public int SoSao
        {
            get; set;
        }

        [StringLength(255)]
        public string BinhLuan { get; set; } = null!;

        [Column(TypeName = "datetime")]
        public DateTime NgayLap
        {
            get; set;
        }

        [ForeignKey("MaNd")]
        [InverseProperty("Danhgia")]
        public virtual Nguoidung MaNdNavigation { get; set; } = null!;

        [ForeignKey("MaSp")]
        [InverseProperty("Danhgia")]
        public virtual Sanpham MaSpNavigation { get; set; } = null!;
    }
}
