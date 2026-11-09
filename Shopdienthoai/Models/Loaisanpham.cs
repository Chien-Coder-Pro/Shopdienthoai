using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("LOAISANPHAM")]
    public partial class Loaisanpham
    {
        [Key]
        [Column("MaLSP")]
        public int MaLsp
        {
            get; set;
        }

        [Column("TenLSP")]
        [StringLength(70)]
        public string TenLsp { get; set; } = null!;

        [StringLength(200)]
        [Unicode(false)]
        public string HinhAnh { get; set; } = null!;

        [StringLength(200)]
        public string Mota { get; set; } = null!;

        [InverseProperty("MaLspNavigation")]
        public virtual ICollection<Sanpham> Sanphams { get; set; } = new List<Sanpham>();
    }
}
