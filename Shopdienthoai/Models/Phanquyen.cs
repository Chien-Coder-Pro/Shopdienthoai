using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    [Table("PHANQUYEN")]
    public partial class Phanquyen
    {
        [Key]
        public int MaQuyen
        {
            get; set;
        }

        [StringLength(20)]
        public string TenQuyen { get; set; } = null!;

        [StringLength(255)]
        public string ChiTietQuyen { get; set; } = null!;

        [InverseProperty("MaQuyenNavigation")]
        public virtual ICollection<Nguoidung> Nguoidungs { get; set; } = new List<Nguoidung>();
    }
}
