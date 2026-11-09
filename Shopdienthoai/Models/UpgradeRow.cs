using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopdienthoai.Models
{
    public class UpgradeRow
    {
        public Nguoidung Customer { get; set; } = null!;
        public decimal ChiTieu
        {
            get; set;
        }
        public Hangkhachhang? DeXuat
        {
            get; set;
        }
    }
}
