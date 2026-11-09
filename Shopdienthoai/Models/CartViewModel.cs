using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopdienthoai.Models
{
    public class CartViewModel
    {
        public List<CartLine> Items { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public decimal TongTien => Items.Sum(x => x.ThanhTien);
    }
}
