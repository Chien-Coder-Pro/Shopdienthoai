using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;
using System.Data;
namespace Shopdienthoai.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ShopDbContext _context;
        private readonly IConfiguration _config;
        private readonly MomoClient _momo;
        private readonly ILogger<PaymentController> _logger;
        private Nguoidung? CurrentUser;
        private bool IsAdmin => CurrentUser?.MaQuyenNavigation.TenQuyen == "Admin";

        public PaymentController(ShopDbContext context, IConfiguration config, MomoClient momo, ILogger<PaymentController> logger)
        {
            _context = context;
            _config = config;
            _momo = momo;
            _logger = logger;
        }

        private async Task GetData()
        {
            if (int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
                CurrentUser = await _context.Nguoidungs.Include(x => x.MaQuyenNavigation).Include(x => x.MaHangNavigation).FirstOrDefaultAsync(x => x.MaNd == id && x.TrangThai == 1);
            if (User.Identity?.IsAuthenticated == true && CurrentUser == null)
            {
                await HttpContext.SignOutAsync("Shop");
                HttpContext.Session.Clear();
            }
        }

        private IActionResult LoginRedirect() => RedirectToAction("Login", "Customer", new { returnUrl = Request.Path.ToString() + Request.QueryString });
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Start(int id)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            if (!MomoClient.Ready(_config))
            {
                TempData["Error"] = "MoMo Sandbox chưa được cấu hình.";
                return Go(id);
            }
            Giaodichthanhtoan g;
            await using (var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                var hd = await _context.Hoadons.FirstOrDefaultAsync(x => x.MaHd == id && x.MaNd == CurrentUser.MaNd);
                if (hd == null)
                    return NotFound();
                if (hd.PhuongThucTt != "MoMo" || hd.TrangThai == "Đã hủy" || hd.TrangThaiThanhToan == ShopRules.Paid)
                    return Go(id);
                var old = await _context.Giaodichthanhtoans.Where(x => x.MaHd == id && x.TrangThai != "Thất bại").OrderByDescending(x => x.MaGd).FirstOrDefaultAsync();
                if (old != null)
                {
                    if (old.TrangThai != "Thành công" && MomoClient.SafePayUrl(old.PayUrl))
                        return Redirect(old.PayUrl!);
                    TempData["Error"] = "Giao dịch đang được xử lý. Hãy chọn Kiểm tra kết quả thay vì tạo giao dịch mới.";
                    return Go(id);
                }
                var money = hd.TongThanhToan ?? (hd.TongTien - ShopRules.Discount(hd.TongTien, hd.PhanTramGiam));
                if (money < 1000 || money > 50000000 || money != decimal.Truncate(money))
                {
                    TempData["Error"] = "Số tiền nằm ngoài giới hạn MoMo (1.000đ–50.000.000đ).";
                    return Go(id);
                }
                g = new Giaodichthanhtoan { MaHd = id, OrderId = "CNS" + id + "_" + Guid.NewGuid().ToString("N"), RequestId = Guid.NewGuid().ToString("N"), SoTien = money, TrangThai = "Chờ phản hồi" };
                _context.Add(g);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            try
            {
                var r = await _momo.Create(g);
                await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                await _context.Entry(g).ReloadAsync();
                if (g.TrangThai == "Thành công")
                    return Go(id);
                if (r.ResultCode == 0 && MomoClient.SafePayUrl(r.PayUrl))
                {
                    g.PayUrl = r.PayUrl;
                    g.TrangThai = "Chờ thanh toán";
                    g.NgayCapNhat = ShopRules.Now;
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                    return Redirect(r.PayUrl!);
                }
                g.TrangThai = r.ResultCode.HasValue && r.ResultCode != 0 ? "Thất bại" : "Chờ đối soát";
                g.ResultCode = r.ResultCode;
                g.NgayCapNhat = ShopRules.Now;
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
                TempData["Error"] = "Chưa mở được trang thanh toán MoMo. Mã phản hồi: " + r.ResultCode;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException)
            {
                _logger.LogWarning("MoMo create chưa hoàn tất cho đơn {Id}: {Type}", id, e.GetType().Name);
                TempData["Error"] = "Chưa nhận được kết quả từ MoMo. Dùng Kiểm tra kết quả để đối soát; không tạo đơn mới.";
            }
            return Go(id);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Check(int id)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            var hd = await _context.Hoadons.FirstOrDefaultAsync(x => x.MaHd == id && (x.MaNd == CurrentUser.MaNd || IsAdmin));
            if (hd == null)
                return NotFound();
            var g = await _context.Giaodichthanhtoans.Where(x => x.MaHd == id).OrderByDescending(x => x.MaGd).FirstOrDefaultAsync();
            if (g == null || !MomoClient.Ready(_config))
            {
                TempData["Error"] = "Chưa có giao dịch hoặc chưa cấu hình MoMo.";
                return Go(id);
            }
            try
            {
                var r = await _momo.Query(g);
                await ApplyResult(r);
                TempData["Success"] = "Đã kiểm tra kết quả với MoMo. Mã: " + r.ResultCode;
            }
            catch (Exception e) when (e is HttpRequestException or TaskCanceledException or InvalidOperationException) { TempData["Error"] = "Chưa đối soát được với MoMo. Vui lòng thử lại sau."; }
            return IsAdmin ? RedirectToAction("OrderDetails", "Admin", new
            {
                id
            }) : Go(id);
        }
        // A browser redirect is a display step, never evidence that money was received.
        public IActionResult Return(int id)
        {
            TempData["Success"] = "Đã quay lại từ MoMo. Shop xác nhận thanh toán qua thông báo máy chủ hoặc bước Kiểm tra kết quả.";
            return Go(id);
        }
        [HttpPost, IgnoreAntiforgeryToken]
        public async Task<IActionResult> Ipn([FromBody] MomoResult r)
        {
            if (!_momo.ValidIpn(r))
                return BadRequest();
            if (!await ApplyResult(r))
                return BadRequest();
            return NoContent();
        }
        private async Task<bool> ApplyResult(MomoResult r)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var g = await _context.Giaodichthanhtoans.SingleOrDefaultAsync(x => x.OrderId == r.OrderId);
            if (g == null || g.RequestId != r.RequestId || g.SoTien != r.Amount || !r.ResultCode.HasValue)
                return false;
            var hd = await _context.Hoadons.FindAsync(g.MaHd);
            if (hd == null || hd.PhuongThucTt != "MoMo")
                return false;
            if (g.TrangThai == "Thành công")
            {
                await tx.CommitAsync();
                return true;
            }
            g.ResultCode = r.ResultCode;
            g.NgayCapNhat = ShopRules.Now;
            if (r.ResultCode == 0)
            {
                if (r.TransId <= 0)
                    return false;
                // Payment facts are retained even if a merchant needs to reconcile an unusual order.
                g.TrangThai = "Thành công";
                g.TransId = r.TransId.ToString();
                hd.TrangThaiThanhToan = ShopRules.Paid;
            }
            else if (r.ResultCode is 9000 or 1000 or 7000)
                g.TrangThai = "Chờ đối soát";
            else
                g.TrangThai = "Thất bại";
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            return true;
        }
        private IActionResult Go(int id) => RedirectToAction("OrderDetails", "Customer", new { id });
    }
}
