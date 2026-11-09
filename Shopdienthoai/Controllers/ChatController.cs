using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;

namespace Shopdienthoai.Controllers
{
    public class ChatController : Controller
    {
        private readonly ShopDbContext _context;

        public ChatController(ShopDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        [EnableRateLimiting("Account")]
        public async Task<IActionResult> Message([FromForm] string? message)
        {
            if (string.IsNullOrWhiteSpace(message) || message.Trim().Length > 500)
                return BadRequest(new { error = "Vui lòng nhập tin nhắn tối đa 500 ký tự." });

            var normalized = Normalize(message.Trim());
            if (IsGreeting(normalized))
                return Json(new { answer = "Xin chào! Mình có thể tư vấn điện thoại, gợi ý sản phẩm theo ngân sách, hoặc hướng dẫn bạn xem đơn hàng." });

            if (ContainsAny(normalized, "don hang", "theo doi don", "kiem tra don"))
                return Json(new { answer = "Bạn có thể xem tình trạng đơn hàng tại mục Đơn hàng của tôi. Vui lòng đăng nhập để xem thông tin cá nhân nhé.", link = "/Customer/Orders", linkText = "Xem đơn hàng" });

            if (ContainsAny(normalized, "giao hang", "van chuyen", "ship"))
                return Json(new { answer = "Để được tư vấn phí và thời gian giao hàng theo khu vực, bạn vui lòng liên hệ shop qua số 0384 229 772 nhé." });

            if (ContainsAny(normalized, "bao hanh", "doi tra"))
                return Json(new { answer = "Chính sách bảo hành và đổi trả tùy sản phẩm. Bạn vui lòng liên hệ shop qua số 0384 229 772 để được xác nhận thông tin chính xác nhé." });

            if (ContainsAny(normalized, "thanh toan", "momo", "tra tien"))
                return Json(new { answer = "Các phương thức thanh toán đang khả dụng sẽ được hiển thị ở bước thanh toán. MoMo trên cửa hàng hiện dùng môi trường Sandbox để thử nghiệm." });

            if (IsProductQuestion(normalized))
            {
                var budget = ParseBudget(normalized);
                var products = await _context.Sanphams
                    .Include(x => x.MaKmNavigation)
                    .Where(x => x.TrangThai == 1 && x.SoLuong > 0)
                    .AsNoTracking()
                    .ToListAsync();

                var now = ShopRules.Now;
                var pricedProducts = products
                    .Select(x => new { Product = x, Price = ShopRules.Price(x, now) })
                    .Where(x => !budget.HasValue || x.Price <= budget.Value)
                    .OrderBy(x => x.Price)
                    .Take(3)
                    .ToList();

                if (pricedProducts.Count == 0)
                {
                    var noMatch = budget.HasValue
                        ? "Hiện chưa có sản phẩm còn hàng trong mức ngân sách này. Bạn có thể tăng ngân sách hoặc liên hệ shop để được tư vấn thêm."
                        : "Hiện chưa có sản phẩm còn hàng để gợi ý. Bạn vui lòng liên hệ shop qua số 0384 229 772 nhé.";
                    return Json(new { answer = noMatch });
                }

                var answer = budget.HasValue
                    ? $"Một số điện thoại còn hàng trong ngân sách {ShopRules.Money(budget)} của bạn:"
                    : "Một số điện thoại còn hàng có giá tốt tại shop:";
                return Json(new
                {
                    answer,
                    products = pricedProducts.Select(x => new
                    {
                        id = x.Product.MaSp,
                        name = x.Product.TenSp,
                        price = ShopRules.Money(x.Price)
                    })
                });
            }

            return Json(new { answer = "Mình có thể giúp gợi ý điện thoại theo ngân sách, hướng dẫn xem đơn hàng, hoặc giải đáp về giao hàng, thanh toán và bảo hành. Bạn muốn tìm hiểu điều gì?" });
        }

        private static bool IsGreeting(string message) =>
            message is "hi" or "hello" or "xin chao" or "chao" ||
            message.StartsWith("xin chao ", StringComparison.Ordinal) ||
            message.StartsWith("chao shop", StringComparison.Ordinal);

        private static bool IsProductQuestion(string message) =>
            ContainsAny(message, "tu van", "goi y", "dien thoai", "san pham", "ngan sach", "gia duoi", "iphone", "samsung", "oppo", "xiaomi", "redmi", "vivo", "realme", "pixel", "mua may");

        private static bool ContainsAny(string message, params string[] phrases) =>
            phrases.Any(message.Contains);

        private static string Normalize(string value)
        {
            var decomposed = value.ToLowerInvariant().Replace('đ', 'd').Normalize(NormalizationForm.FormD);
            var result = new StringBuilder(decomposed.Length);
            foreach (var character in decomposed)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                    result.Append(character);
            }
            return result.ToString().Normalize(NormalizationForm.FormC);
        }

        private static decimal? ParseBudget(string message)
        {
            var match = Regex.Match(message, @"(\d+(?:[.,]\d+)?)\s*(trieu|tr|m|nghin|ngan|k|dong|vnd|d)\b");
            if (!match.Success ||
                !decimal.TryParse(match.Groups[1].Value.Replace(',', '.'), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var amount))
                return null;

            var unit = match.Groups[2].Value;
            return unit is "trieu" or "tr" or "m" ? amount * 1_000_000m
                : unit is "nghin" or "ngan" or "k" ? amount * 1_000m
                : amount;
        }
    }
}
