using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;

namespace Shopdienthoai.Controllers
{
    [AdminOnly]
    public class LoaisanphamsController : Controller
    {
        private readonly ShopDbContext _context;
        private readonly IWebHostEnvironment _env;

        public LoaisanphamsController(ShopDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // GET: Loaisanphams
        public async Task<IActionResult> Index()
        {
            return View(await _context.Loaisanphams.OrderBy(x => x.TenLsp).ToListAsync());
        }

        // GET: Loaisanphams/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();
            var loaisanpham = await _context.Loaisanphams.FindAsync(id.Value);
            return loaisanpham == null ? NotFound() : View(loaisanpham);
        }

        // GET: Loaisanphams/Create
        public IActionResult Create()
        {
            return View(new Loaisanpham());
        }

        // POST: Loaisanphams/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("MaLsp,TenLsp,Mota")] Loaisanpham loaisanpham, IFormFile? upload)
        {
            if (loaisanpham.MaLsp != 0)
                return BadRequest();
            await ValidateBrand(loaisanpham);
            loaisanpham.HinhAnh = "logo.png";
            if (ModelState.IsValid && upload != null)
            {
                try
                {
                    loaisanpham.HinhAnh = await Upload(upload);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("HinhAnh", ex.Message);
                }
            }
            if (ModelState.IsValid)
            {
                loaisanpham.TenLsp = loaisanpham.TenLsp.Trim();
                loaisanpham.Mota = loaisanpham.Mota.Trim();
                _context.Loaisanphams.Add(loaisanpham);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã thêm hãng sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            return View(loaisanpham);
        }

        // GET: Loaisanphams/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
                return NotFound();
            var loaisanpham = await _context.Loaisanphams.FindAsync(id.Value);
            return loaisanpham == null ? NotFound() : View(loaisanpham);
        }

        // POST: Loaisanphams/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLsp,TenLsp,Mota")] Loaisanpham loaisanpham, IFormFile? upload)
        {
            if (id != loaisanpham.MaLsp)
                return BadRequest();
            var old = await _context.Loaisanphams.FindAsync(id);
            if (old == null)
                return NotFound();
            await ValidateBrand(loaisanpham);
            loaisanpham.HinhAnh = old.HinhAnh;
            if (ModelState.IsValid && upload != null)
            {
                try
                {
                    loaisanpham.HinhAnh = await Upload(upload);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("HinhAnh", ex.Message);
                }
            }
            if (ModelState.IsValid)
            {
                old.TenLsp = loaisanpham.TenLsp.Trim();
                old.Mota = loaisanpham.Mota.Trim();
                old.HinhAnh = loaisanpham.HinhAnh;
                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LoaisanphamExists(id))
                        return NotFound();
                    throw;
                }
                TempData["Success"] = "Đã cập nhật hãng sản phẩm.";
                return RedirectToAction(nameof(Index));
            }
            return View(loaisanpham);
        }

        // GET: Loaisanphams/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
                return NotFound();
            var loaisanpham = await _context.Loaisanphams.FindAsync(id.Value);
            if (loaisanpham == null)
                return NotFound();
            ViewBag.HasProducts = await _context.Sanphams.AnyAsync(x => x.MaLsp == id);
            return View(loaisanpham);
        }

        // POST: Loaisanphams/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var loaisanpham = await _context.Loaisanphams.FindAsync(id);
            if (loaisanpham == null)
                return NotFound();
            if (await _context.Sanphams.AnyAsync(x => x.MaLsp == id))
            {
                TempData["Error"] = "Hãng đang có sản phẩm, không thể xóa.";
            }
            else
            {
                _context.Loaisanphams.Remove(loaisanpham);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã xóa hãng.";
            }
            return RedirectToAction(nameof(Index));
        }

        private bool LoaisanphamExists(int id)
        {
            return _context.Loaisanphams.Any(x => x.MaLsp == id);
        }

        private async Task ValidateBrand(Loaisanpham loaisanpham)
        {
            ModelState.Remove(nameof(loaisanpham.HinhAnh));
            var name = (loaisanpham.TenLsp ?? "").Trim();
            if (await _context.Loaisanphams.AnyAsync(x => x.MaLsp != loaisanpham.MaLsp && x.TenLsp == name))
                ModelState.AddModelError("TenLsp", "Tên hãng đã có.");
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
