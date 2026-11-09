using System.Globalization;
using System.Security.Cryptography;
using System.Text;
namespace Shopdienthoai.Models
{
    public static class ShopRules
    {
        public const string Unpaid = "Chưa thanh toán";
        public const string Paid = "Đã thanh toán";
        public static DateTime Now => DateTime.UtcNow.AddHours(7);
        public static string Money(decimal? n) => (n ?? 0).ToString("N0", CultureInfo.GetCultureInfo("vi-VN")) + "đ";
        public static bool Active(Khuyenmai? k, DateTime now) => k != null && k.LoaiKm != "Nothing" && k.NgayBd <= now && now <= k.NgayKt;
        // GiamGia and GiaReOnline are fixed VND discounts in the supplied database.
        public static decimal Price(Sanpham p, DateTime now) => Active(p.MaKmNavigation, now) && p.MaKmNavigation!.LoaiKm is "GiamGia" or "GiaReOnline" ? Math.Max(0, p.DonGia - p.MaKmNavigation.GiaTriKm) : p.DonGia;
        public static decimal Discount(decimal total, decimal percent) => Math.Round(total * percent / 100m, 0, MidpointRounding.AwayFromZero);
        public static string Hmac(string secret, string raw) => Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(secret), Encoding.UTF8.GetBytes(raw))).ToLowerInvariant();
        public static bool SameSignature(string a, string b)
        {
            try
            {
                return CryptographicOperations.FixedTimeEquals(Convert.FromHexString(a), Convert.FromHexString(b));
            }
            catch (FormatException) { return false; }
        }
        public static string Image(string? name) => "/image/" + (string.IsNullOrWhiteSpace(name) ? "logo.png" : Uri.EscapeDataString(Path.GetFileName(name)));
    }
}
