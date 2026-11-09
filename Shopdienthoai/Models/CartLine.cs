using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopdienthoai.Models
{
    public class CartLine
    {
        public Sanpham SanPham { get; set; } = null!;
        public int SoLuong
        {
            get; set;
        }
        public decimal DonGia
        {
            get; set;
        }
        public decimal ThanhTien => DonGia * SoLuong;
    }
}
