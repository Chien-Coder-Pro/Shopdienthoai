using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopdienthoai.Models
{
    public class CartItem
    {
        public int MaSp
        {
            get; set;
        }
        public int SoLuong
        {
            get; set;
        }
    }
}
