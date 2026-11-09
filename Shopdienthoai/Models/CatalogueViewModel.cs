using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Shopdienthoai.Models
{
    public class CatalogueViewModel
    {
        public List<Sanpham> Items { get; set; } = new();
        public string? Keyword
        {
            get; set;
        }
        public int? Brand
        {
            get; set;
        }
        public decimal? Min
        {
            get; set;
        }
        public decimal? Max
        {
            get; set;
        }
        public string Sort { get; set; } = "new";
        public int Page
        {
            get; set;
        }
        public int Total
        {
            get; set;
        }
        public int Pages => Math.Max(1, (int)Math.Ceiling(Total / 12m));
    }
}
