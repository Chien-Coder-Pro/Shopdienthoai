using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;

namespace Shopdienthoai.Controllers
{
    [AdminOnly]
    public class SanphamsController : Controller
    {
        private readonly ShopDbContext _context;
        private readonly IWebHostEnvironment _env;

        public SanphamsController(ShopDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Sanphams
        public async Task<IActionResult> Index(string? keyword)
        {
            var products = _context.Sanphams.Include(x => x.MaLspNavigation)
                .Include(x => x.MaKmNavigation).AsQueryable();
            if (!string.IsNullOrWhiteSpace(keyword))
                products = products.Where(x => x.TenSp.Contains(keyword));
            ViewBag.Keyword = keyword;
            return View(await products.OrderByDescending(x => x.MaSp).ToListAsync());
        }

        // GET: Sanphams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var sanpham = await _context.Sanphams.Include(x => x.MaLspNavigation)
                .Include(x => x.MaKmNavigation).FirstOrDefaultAsync(x => x.MaSp == id);
            return sanpham == null ? NotFound() : View(sanpham);
        }

        // GET: Sanphams/Create
        public async Task<IActionResult> Create()
        {
            var sanpham = new Sanpham
            {
                SoLuong = 1,
                TrangThai = 1,
                MaKm = await _context.Khuyenmais.Where(x => x.LoaiKm == "Nothing")
                    .Select(x => x.MaKm).FirstAsync()
            };
            await LoadSelectLists(sanpham.MaLsp, sanpham.MaKm);
            return View(sanpham);
        }

        // POST: Sanphams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaSp,MaLsp,TenSp,DonGia,SoLuong,MaKm,ManHinh,Hdh,CamSau,CamTruoc,Cpu,Ram,Rom,Sdcard,Pin,TrangThai")] Sanpham sanpham, IFormFile? upload)
        {
            if (sanpham.MaSp != 0)
                return BadRequest();
            await ValidateProduct(sanpham);
            if (upload == null)
                ModelState.AddModelError("HinhAnh", "Vui lòng chọn ảnh sản phẩm.");
            if (ModelState.IsValid && upload != null)
            {
                try
                {
                    sanpham.HinhAnh = await Upload(upload);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("HinhAnh", ex.Message);
                }
            }
            if (ModelState.IsValid)
            {
                sanpham.SoSao = 0;
                sanpham.SoDanhGia = 0;
                _context.Sanphams.Add(sanpham);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            await LoadSelectLists(sanpham.MaLsp, sanpham.MaKm);
            return View(sanpham);
        }

        // GET: Sanphams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            var sanpham = await _context.Sanphams.FindAsync(id.Value);
            if (sanpham == null)
                return NotFound();
            await LoadSelectLists(sanpham.MaLsp, sanpham.MaKm);
            return View(sanpham);
        }

        // POST: Sanphams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaSp,MaLsp,TenSp,DonGia,SoLuong,MaKm,ManHinh,Hdh,CamSau,CamTruoc,Cpu,Ram,Rom,Sdcard,Pin,TrangThai")] Sanpham sanpham, IFormFile? upload)
        {
            if (id != sanpham.MaSp)
                return BadRequest();
            var old = await _context.Sanphams.FindAsync(id);
            if (old == null)
                return NotFound();
            await ValidateProduct(sanpham);
            sanpham.HinhAnh = old.HinhAnh;
            if (ModelState.IsValid && upload != null)
            {
                try
                {
                    sanpham.HinhAnh = await Upload(upload);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("HinhAnh", ex.Message);
                }
            }
            if (ModelState.IsValid)
            {
                // Chỉ cập nhật các trường được sửa; giữ nguyên đánh giá và lịch sử hóa đơn.
                old.MaLsp = sanpham.MaLsp;
                old.TenSp = sanpham.TenSp;
                old.DonGia = sanpham.DonGia;
                old.SoLuong = sanpham.SoLuong;
                old.MaKm = sanpham.MaKm;
                old.HinhAnh = sanpham.HinhAnh;
                old.ManHinh = sanpham.ManHinh;
                old.Hdh = sanpham.Hdh;
                old.CamSau = sanpham.CamSau;
                old.CamTruoc = sanpham.CamTruoc;
                old.Cpu = sanpham.Cpu;
                old.Ram = sanpham.Ram;
                old.Rom = sanpham.Rom;
                old.Sdcard = sanpham.Sdcard;
                old.Pin = sanpham.Pin;
                old.TrangThai = sanpham.TrangThai;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SanphamExists(id))
                        return NotFound();
                    throw;
                }
                TempData["Success"] = "Đã cập nhật sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            await LoadSelectLists(sanpham.MaLsp, sanpham.MaKm);
            return View(sanpham);
        }

        // GET: Sanphams/Delete/5 - chỉ hiển thị xác nhận.
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var sanpham = await _context.Sanphams.Include(x => x.MaLspNavigation)
                .FirstOrDefaultAsync(x => x.MaSp == id);
            return sanpham == null ? NotFound() : View(sanpham);
        }

        // POST: Sanphams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var sanpham = await _context.Sanphams.FindAsync(id);
            if (sanpham == null)
                return NotFound();
            sanpham.TrangThai = 0;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã ngừng bán sản phẩm. Dữ liệu hóa đơn được giữ nguyên.";
            return RedirectToAction(nameof(Index));
        }

        private bool SanphamExists(int id)
        {
            return _context.Sanphams.Any(x => x.MaSp == id);
        }

        private async Task LoadSelectLists(int? brand = null, int? promotion = null)
        {
            ViewData["MaLsp"] = new SelectList(await _context.Loaisanphams.OrderBy(x => x.TenLsp).ToListAsync(), "MaLsp", "TenLsp", brand);
            ViewData["MaKm"] = new SelectList(await _context.Khuyenmais.OrderBy(x => x.MaKm).ToListAsync(), "MaKm", "TenKm", promotion);
        }

        private async Task ValidateProduct(Sanpham sanpham)
        {
            ModelState.Remove(nameof(sanpham.HinhAnh));
            ModelState.Remove(nameof(sanpham.MaKmNavigation));
            ModelState.Remove(nameof(sanpham.MaLspNavigation));
            if (sanpham.DonGia < 0 || sanpham.DonGia > 99999999999999m || sanpham.SoLuong < 0 || sanpham.TrangThai is not (0 or 1))
                ModelState.AddModelError("", "Giá, tồn kho hoặc trạng thái chưa hợp lệ.");
            if (!await _context.Loaisanphams.AnyAsync(x => x.MaLsp == sanpham.MaLsp) || !await _context.Khuyenmais.AnyAsync(x => x.MaKm == sanpham.MaKm))
                ModelState.AddModelError("", "Hãng hoặc khuyến mãi không tồn tại.");
        }

        // Xử lý upload bên trong Controller như bài thực hành.
        private async Task<string> Upload(IFormFile file)
        {
            if (file.Length == 0 || file.Length > 5 * 1024 * 1024)
                throw new InvalidOperationException("Ảnh phải có dung lượng từ 1 byte đến 5MB.");
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (ext is not (".jpg" or ".jpeg" or ".png" or ".webp"))
                throw new InvalidOperationException("Chỉ nhận JPG, PNG hoặc WebP.");
            await using var input = file.OpenReadStream();
            var header = new byte[12];
            var count = 0;
            while (count < header.Length)
            {
                var read = await input.ReadAsync(header.AsMemory(count));
                if (read == 0)
                    break;
                count += read;
            }
            bool valid;
            if (ext is ".jpg" or ".jpeg")
            {
                valid = count >= 3 && header[0] == 255 && header[1] == 216 && header[2] == 255;
            }
            else if (ext == ".png")
            {
                var pngHeader = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 };
                valid = count >= 8 && header.AsSpan(0, 8).SequenceEqual(pngHeader);
            }
            else
            {
                valid = count == 12
                    && System.Text.Encoding.ASCII.GetString(header, 0, 4) == "RIFF"
                    && System.Text.Encoding.ASCII.GetString(header, 8, 4) == "WEBP";
            }
            if (!valid)
                throw new InvalidOperationException("Nội dung file không đúng định dạng ảnh.");
            var name = Guid.NewGuid().ToString("N") + ext;
            var folder = Path.Combine(_env.WebRootPath, "image");
            Directory.CreateDirectory(folder);
            await using var output = System.IO.File.Create(Path.Combine(folder, name));
            await output.WriteAsync(header.AsMemory(0, count));
            await input.CopyToAsync(output);
            return name;
        }
    }
}
