using System.ComponentModel.DataAnnotations;

namespace Shopdienthoai.Models
{
    public class GooglePending
    {
        public string Id { get; set; } = "";
        public string Email { get; set; } = "";
        public string Ho { get; set; } = "";
        public string Ten { get; set; } = "";
        public DateTime Expires
        {
            get; set;
        }
    }
}
