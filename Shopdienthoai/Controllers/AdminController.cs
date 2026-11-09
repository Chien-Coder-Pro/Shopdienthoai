using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;
using System.Data;
namespace Shopdienthoai.Controllers
{
    [AdminOnly]
    public class AdminController : Controller
    {
        private readonly ShopDbContext _context;
        private Nguoidung? CurrentUser => ViewBag.CurrentUser as Nguoidung;

        public AdminController(ShopDbContext context)
        {
            _context = context;
        }
        public async Task<IActionResult> Index(DateTime? from, DateTime? to)
        {
            var start = (from ?? new DateTime(ShopRules.Now.Year, ShopRules.Now.Month, 1)).Date;
            var end = (to ?? ShopRules.Now).Date.AddDays(1);
            if (end <= start)
                end = start.AddDays(1);
            ViewBag.From = start;
            ViewBag.To = end.AddDays(-1);
            var orders = await _context.Hoadons.Where(x => x.NgayLap >= start && x.NgayLap < end).ToListAsync();
            ViewBag.Revenue = orders.Where(x => x.TrangThai == "Đã giao hàng" && x.TrangThaiThanhToan == ShopRules.Paid).Sum(x => x.TongThanhToan ?? 0);
            ViewBag.OrderCount = orders.Count;
            ViewBag.Waiting = await _context.Hoadons.CountAsync(x => x.TrangThai == "Chờ xử lý");
            ViewBag.CustomerCount = await _context.Nguoidungs.CountAsync(x => x.MaQuyenNavigation.TenQuyen == "Customer");
            ViewBag.LowStock = await _context.Sanphams.Where(x => x.TrangThai == 1 && x.SoLuong <= 5).OrderBy(x => x.SoLuong).Take(10).ToListAsync();
            var ids = orders.Where(x => x.TrangThai == "Đã giao hàng" && x.TrangThaiThanhToan == ShopRules.Paid).Select(x => x.MaHd).ToList();
            var details = await _context.Chitiethoadons.Include(x => x.MaSpNavigation).Where(x => ids.Contains(x.MaHd)).ToListAsync();
            ViewBag.Top = details.GroupBy(x => x.MaSpNavigation.TenSp).Select(g => new TopProduct { Ten = g.Key, SoLuong = g.Sum(x => x.SoLuong) }).OrderByDescending(x => x.SoLuong).Take(5).ToList();
            ViewBag.UpgradeCount = (await UpgradeRows()).Count(x => x.DeXuat != null);
            return View();
        }
        public async Task<IActionResult> Orders(string? status, string? keyword)
        {
            var q = _context.Hoadons.Include(x => x.MaNdNavigation).AsQueryable();
            if (!string.IsNullOrEmpty(status))
                q = q.Where(x => x.TrangThai == status);
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                keyword = keyword.Trim();
                q = q.Where(x => x.NguoiNhan.Contains(keyword) || x.Sdt.Contains(keyword) || x.MaHd.ToString() == keyword);
            }
            ViewBag.Status = status;
            ViewBag.Keyword = keyword;
            return View(await q.OrderByDescending(x => x.MaHd).ToListAsync());
        }
        public async Task<IActionResult> OrderDetails(int id)
        {
            var hd = await _context.Hoadons.Include(x => x.MaNdNavigation).Include(x => x.Chitiethoadons).ThenInclude(x => x.MaSpNavigation).FirstOrDefaultAsync(x => x.MaHd == id);
            if (hd == null)
                return NotFound();
            ViewBag.Payments = await _context.Giaodichthanhtoans.Where(x => x.MaHd == id).OrderByDescending(x => x.MaGd).ToListAsync();
            return View(hd);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateOrder(int id, string status)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var hd = await _context.Hoadons.Include(x => x.Chitiethoadons).FirstOrDefaultAsync(x => x.MaHd == id);
            if (hd == null)
                return NotFound();
            var next = hd.TrangThai switch
            {
                "Chờ xử lý" => new[] { "Đã xác nhận", "Đã hủy" },
                "Đã xác nhận" => new[] { "Đang giao hàng", "Đã hủy" },
                "Đang giao hàng" => new[] { "Đã giao hàng" },
                _ => Array.Empty<string>()
            };
            if (!next.Contains(status))
            {
                TempData["Error"] = "Bước chuyển trạng thái không hợp lệ.";
                return RedirectToAction(nameof(OrderDetails), new
                {
                    id
                });
            }
            if (status == "Đã hủy")
            {
                if (hd.TrangThaiThanhToan == ShopRules.Paid || await _context.Giaodichthanhtoans.AnyAsync(x => x.MaHd == id && x.TrangThai != "Thất bại"))
                {
                    TempData["Error"] = "Không hủy đơn đã thanh toán hoặc đang chờ MoMo. Cần đối soát và xử lý hoàn tiền riêng.";
                    return RedirectToAction(nameof(OrderDetails), new
                    {
                        id
                    });
                }
                foreach (var ct in hd.Chitiethoadons.OrderBy(x => x.MaSp))
                    await _context.Sanphams.Where(x => x.MaSp == ct.MaSp).ExecuteUpdateAsync(s => s.SetProperty(x => x.SoLuong, x => x.SoLuong + ct.SoLuong));
            }
            if (status == "Đang giao hàng" && hd.PhuongThucTt == "MoMo" && hd.TrangThaiThanhToan != ShopRules.Paid)
            {
                TempData["Error"] = "Đơn MoMo phải thanh toán thành công trước khi giao.";
                return RedirectToAction(nameof(OrderDetails), new
                {
                    id
                });
            }
            hd.TrangThai = status;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = "Đã cập nhật trạng thái đơn.";
            return RedirectToAction(nameof(OrderDetails), new
            {
                id
            });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmPaid(int id)
        {
            var changed = await _context.Hoadons.Where(x => x.MaHd == id && x.PhuongThucTt == "Trực tiếp" && x.TrangThai != "Đã hủy" && x.TrangThaiThanhToan != ShopRules.Paid).ExecuteUpdateAsync(s => s.SetProperty(x => x.TrangThaiThanhToan, ShopRules.Paid));
            TempData[changed == 1 ? "Success" : "Error"] = changed == 1 ? "Đã xác nhận thu tiền trực tiếp." : "Không thể xác nhận thủ công cho đơn này.";
            return RedirectToAction(nameof(OrderDetails), new
            {
                id
            });
        }
        private async Task<List<UpgradeRow>> UpgradeRows()
        {
            var customers = await _context.Nguoidungs.Include(x => x.MaHangNavigation).Include(x => x.MaQuyenNavigation).Where(x => x.MaQuyenNavigation.TenQuyen == "Customer").ToListAsync();
            var amounts = await _context.Hoadons.Where(x => x.TrangThai == "Đã giao hàng" && x.TrangThaiThanhToan == ShopRules.Paid).Select(x => new { x.MaNd, x.TongThanhToan }).ToListAsync();
            var spent = amounts.GroupBy(x => x.MaNd).ToDictionary(g => g.Key, g => g.Sum(x => x.TongThanhToan ?? 0));
            var ranks = await _context.Hangkhachhangs.OrderByDescending(x => x.NguongChiTieu).ToListAsync();
            return customers.Select(u => { var total = spent.GetValueOrDefault(u.MaNd); var r = ranks.FirstOrDefault(x => x.NguongChiTieu <= total && x.NguongChiTieu > u.MaHangNavigation.NguongChiTieu); return new UpgradeRow { Customer = u, ChiTieu = total, DeXuat = r }; }).ToList();
        }
        public async Task<IActionResult> Customers(string? keyword, bool eligible = false)
        {
            var rows = await UpgradeRows();
            if (!string.IsNullOrWhiteSpace(keyword))
                rows = rows.Where(x => (x.Customer.Ho + " " + x.Customer.Ten + " " + x.Customer.Email + " " + x.Customer.TaiKhoan).Contains(keyword, StringComparison.OrdinalIgnoreCase)).ToList();
            if (eligible)
                rows = rows.Where(x => x.DeXuat != null).ToList();
            ViewBag.Eligible = eligible;
            ViewBag.Keyword = keyword;
            return View(rows);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUpgrade(int id)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var row = (await UpgradeRows()).FirstOrDefault(x => x.Customer.MaNd == id);
            if (row?.DeXuat == null)
            {
                TempData["Error"] = "Khách chưa đủ điều kiện hoặc đã được nâng hạng.";
                return RedirectToAction(nameof(Customers));
            }
            row.Customer.MaHang = row.DeXuat.MaHang;
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = "Đã duyệt nâng lên hạng " + row.DeXuat.TenHang + ". Áp dụng từ đơn tiếp theo.";
            return RedirectToAction(nameof(Customers), new
            {
                eligible = true
            });
        }
        public async Task<IActionResult> Users(string? keyword)
        {
            var q = _context.Nguoidungs.Include(x => x.MaHangNavigation).Include(x => x.MaQuyenNavigation).AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(x => x.TaiKhoan.Contains(keyword) || x.Email.Contains(keyword));
            ViewBag.Keyword = keyword;
            return View(await q.OrderBy(x => x.MaNd).ToListAsync());
        }
        public async Task<IActionResult> EditUser(int id)
        {
            var u = await _context.Nguoidungs.FindAsync(id);
            if (u == null)
                return NotFound();
            ViewData["Ranks"] = new SelectList(await _context.Hangkhachhangs.OrderBy(x => x.NguongChiTieu).ToListAsync(), "MaHang", "TenHang", u.MaHang);
            ViewData["Roles"] = new SelectList(await _context.Phanquyens.ToListAsync(), "MaQuyen", "TenQuyen", u.MaQuyen);
            return View(u);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, int rank, int role, int state)
        {
            var u = await _context.Nguoidungs.FindAsync(id);
            if (u == null)
                return NotFound();
            if (!await _context.Hangkhachhangs.AnyAsync(x => x.MaHang == rank) || !await _context.Phanquyens.AnyAsync(x => x.MaQuyen == role) || state is not (0 or 1))
                return BadRequest();
            if (u.MaNd == CurrentUser!.MaNd && (u.MaQuyen != role || state == 0))
            {
                TempData["Error"] = "Không thể tự khóa hoặc tự thay đổi quyền của tài khoản đang dùng.";
                return RedirectToAction(nameof(EditUser), new
                {
                    id
                });
            }
            u.MaHang = rank;
            u.MaQuyen = role;
            u.TrangThai = state;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật người dùng.";
            return RedirectToAction(nameof(Users));
        }
        public async Task<IActionResult> Ranks() => View(await _context.Hangkhachhangs.OrderBy(x => x.NguongChiTieu).ToListAsync());
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveRank(int id, decimal threshold, decimal discount)
        {
            var r = await _context.Hangkhachhangs.FindAsync(id);
            if (r == null)
                return NotFound();
            if (threshold < 0 || threshold > 9999999999999999m || discount < 0 || discount > 100 || (r.TenHang == "Thường" && threshold != 0))
                TempData["Error"] = "Ngưỡng phải không âm, giảm giá từ 0–100%; hạng Thường có ngưỡng 0.";
            else if (await _context.Hangkhachhangs.AnyAsync(x => x.MaHang != id && x.NguongChiTieu == threshold))
                TempData["Error"] = "Hai hạng không được có cùng ngưỡng chi tiêu.";
            else
            {
                r.NguongChiTieu = threshold;
                r.PhanTramGiam = discount;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã cập nhật chính sách. Các hóa đơn cũ giữ nguyên ưu đãi đã chốt.";
            }
            return RedirectToAction(nameof(Ranks));
        }
        public async Task<IActionResult> Reviews() => View(await _context.Danhgia.Include(x => x.MaSpNavigation).Include(x => x.MaNdNavigation).OrderByDescending(x => x.NgayLap).ToListAsync());
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var r = await _context.Danhgia.FindAsync(id);
            if (r == null)
                return NotFound();
            var productId = r.MaSp;
            _context.Remove(r);
            await _context.SaveChangesAsync();
            var p = await _context.Sanphams.FindAsync(productId);
            var stars = await _context.Danhgia.Where(x => x.MaSp == productId).Select(x => x.SoSao).ToListAsync();
            if (p != null)
            {
                p.SoDanhGia = stars.Count;
                p.SoSao = stars.Count == 0 ? 0 : (int)Math.Round(stars.Average(), MidpointRounding.AwayFromZero);
                await _context.SaveChangesAsync();
            }
            await tx.CommitAsync();
            TempData["Success"] = "Đã xóa nhận xét và cập nhật điểm đánh giá.";
            return RedirectToAction(nameof(Reviews));
        }
        public async Task<IActionResult> Settings() => View(await _context.Emailhangdois.OrderByDescending(x => x.MaEmail).Take(100).ToListAsync());
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RetryEmail(int id)
        {
            var e = await _context.Emailhangdois.FindAsync(id);
            if (e == null || e.DaGuiLuc != null)
                return BadRequest();
            e.SoLanThu = 0;
            e.ThuTiepLuc = null;
            e.LoiGanNhat = null;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã đưa thư về hàng đợi. Thư sẽ gửi khi SMTP được cấu hình và bật.";
            return RedirectToAction(nameof(Settings));
        }
    }
}
