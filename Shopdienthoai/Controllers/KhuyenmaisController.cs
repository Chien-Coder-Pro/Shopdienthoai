using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;

namespace Shopdienthoai.Controllers
{
    [AdminOnly]
    public class KhuyenmaisController : Controller
    {
        private readonly ShopDbContext _context;

        public KhuyenmaisController(ShopDbContext context)
        {
            _context = context;
        }

        // GET: Khuyenmais
        public async Task<IActionResult> Index()
        {
            return View(await _context.Khuyenmais.OrderByDescending(x => x.MaKm).ToListAsync());
        }

        // GET: Khuyenmais/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var khuyenmai = await _context.Khuyenmais.FindAsync(id.Value);
            return khuyenmai == null ? NotFound() : View(khuyenmai);
        }

        // GET: Khuyenmais/Create
        public IActionResult Create()
        {
            var khuyenmai = new Khuyenmai { NgayBd = ShopRules.Now, NgayKt = ShopRules.Now.AddDays(7), LoaiKm = "GiamGia" };
            LoadSelectList(khuyenmai.LoaiKm);
            return View(khuyenmai);
        }

        // POST: Khuyenmais/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaKm,TenKm,LoaiKm,GiaTriKm,NgayBd,NgayKt")] Khuyenmai khuyenmai)
        {
            if (khuyenmai.MaKm != 0)
                return BadRequest();
            ValidatePromotion(khuyenmai);
            if (ModelState.IsValid)
            {
                if (khuyenmai.LoaiKm is not ("GiamGia" or "GiaReOnline"))
                    khuyenmai.GiaTriKm = 0;
                khuyenmai.TenKm = khuyenmai.TenKm.Trim();
                _context.Khuyenmais.Add(khuyenmai);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm khuyến mãi.";
                return RedirectToAction(nameof(Index));
            }
            LoadSelectList(khuyenmai.LoaiKm);
            return View(khuyenmai);
        }

        // GET: Khuyenmais/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            var khuyenmai = await _context.Khuyenmais.FindAsync(id.Value);
            if (khuyenmai == null)
                return NotFound();
            LoadSelectList(khuyenmai.LoaiKm);
            return View(khuyenmai);
        }

        // POST: Khuyenmais/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaKm,TenKm,LoaiKm,GiaTriKm,NgayBd,NgayKt")] Khuyenmai khuyenmai)
        {
            if (id != khuyenmai.MaKm)
                return BadRequest();
            var old = await _context.Khuyenmais.FindAsync(id);
            if (old == null)
                return NotFound();
            ValidatePromotion(khuyenmai);
            if (old.LoaiKm == "Nothing" && khuyenmai.LoaiKm != "Nothing")
                ModelState.AddModelError("", "Giữ loại Không khuyến mãi cho bản ghi mặc định.");
            if (ModelState.IsValid)
            {
                if (khuyenmai.LoaiKm is not ("GiamGia" or "GiaReOnline"))
                    khuyenmai.GiaTriKm = 0;
                old.TenKm = khuyenmai.TenKm.Trim();
                old.LoaiKm = khuyenmai.LoaiKm;
                old.GiaTriKm = khuyenmai.GiaTriKm;
                old.NgayBd = khuyenmai.NgayBd;
                old.NgayKt = khuyenmai.NgayKt;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KhuyenmaiExists(id))
                        return NotFound();
                    throw;
                }
                TempData["Success"] = "Đã cập nhật khuyến mãi. Đơn đã đặt giữ nguyên giá cũ.";
                return RedirectToAction(nameof(Index));
            }
            LoadSelectList(khuyenmai.LoaiKm);
            return View(khuyenmai);
        }

        // GET: Khuyenmais/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var khuyenmai = await _context.Khuyenmais.FindAsync(id.Value);
            if (khuyenmai == null)
                return NotFound();
            ViewBag.CannotDelete = khuyenmai.LoaiKm == "Nothing" || await _context.Sanphams.AnyAsync(x => x.MaKm == id);
            return View(khuyenmai);
        }

        // POST: Khuyenmais/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var khuyenmai = await _context.Khuyenmais.FindAsync(id);
            if (khuyenmai == null)
                return NotFound();
            if (khuyenmai.LoaiKm == "Nothing" || await _context.Sanphams.AnyAsync(x => x.MaKm == id))
            {
                TempData["Error"] = "Không xóa khuyến mãi mặc định hoặc đang được sản phẩm sử dụng.";
            }
            else
            {
                _context.Khuyenmais.Remove(khuyenmai);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa khuyến mãi.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool KhuyenmaiExists(int id)
        {
            return _context.Khuyenmais.Any(x => x.MaKm == id);
        }

        private void ValidatePromotion(Khuyenmai khuyenmai)
        {
            if (!new[] { "Nothing", "GiamGia", "GiaReOnline", "TraGop", "MoiRaMat" }.Contains(khuyenmai.LoaiKm)
                || khuyenmai.GiaTriKm < 0 || khuyenmai.GiaTriKm > 99999999999999m || khuyenmai.NgayKt < khuyenmai.NgayBd)
                ModelState.AddModelError("", "Loại, giá trị hoặc thời gian khuyến mãi chưa hợp lệ.");
        }

        private void LoadSelectList(string selected)
        {
            ViewData["LoaiKm"] = new SelectList(new[]
            {
                new { Value = "Nothing", Text = "Không khuyến mãi" },
                new { Value = "GiamGia", Text = "Giảm giá" },
                new { Value = "GiaReOnline", Text = "Giá rẻ online" },
                new { Value = "TraGop", Text = "Nhãn trả góp" },
                new { Value = "MoiRaMat", Text = "Nhãn mới ra mắt" }
            }, "Value", "Text", selected);
        }
    }
}
