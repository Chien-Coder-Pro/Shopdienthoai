using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Data;

namespace Shopdienthoai.Controllers
{
    public class CustomerController : Controller
    {
        private readonly ShopDbContext _context;
        private readonly IConfiguration _config;
        private readonly IPasswordHasher<Nguoidung> _hasher;
        private Nguoidung? CurrentUser;
        private bool IsAdmin => CurrentUser?.MaQuyenNavigation.TenQuyen == "Admin";

        public CustomerController(ShopDbContext context, IConfiguration config, IPasswordHasher<Nguoidung> hasher)
        {
            _context = context;
            _config = config;
            _hasher = hasher;
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
            // Khôi phục giỏ đã lưu nếu Session hết hạn nhưng cookie đăng nhập còn hiệu lực.
            if (CurrentUser != null && HttpContext.Session.GetString("cartOwner") != CurrentUser.MaNd.ToString())
            {
                var saved = await _context.Giohangs.AsNoTracking().Where(x => x.MaNd == CurrentUser.MaNd).Select(x => new CartItem { MaSp = x.MaSp, SoLuong = x.SoLuong }).ToListAsync();
                SaveCartSession(saved);
                HttpContext.Session.SetString("cartOwner", CurrentUser.MaNd.ToString());
            }
            // Dữ liệu dùng chung cho giao diện, tương tự GetData() trong mẫu BTTH9.
            ViewBag.CurrentUser = CurrentUser;
            ViewBag.IsAdmin = IsAdmin;
            ViewBag.Brands = await _context.Loaisanphams.OrderBy(x => x.TenLsp).ToListAsync();
            var memberRanks = await _context.Hangkhachhangs.Where(x => x.NguongChiTieu > 0).OrderBy(x => x.NguongChiTieu).ToListAsync();
            ViewBag.MemberOffer = string.Join(" · ", memberRanks.Select(x => x.TenHang + " " + x.PhanTramGiam.ToString("0.##") + "%"));
            ViewBag.CartCount = GetCartItems().Sum(x => x.SoLuong);
            ViewBag.GoogleEnabled = _config.GetValue<bool>("Authentication:Google:Enabled") && !string.IsNullOrWhiteSpace(_config["Authentication:Google:ClientId"]) && !string.IsNullOrWhiteSpace(_config["Authentication:Google:ClientSecret"]);
            ViewBag.MomoEnabled = MomoClient.Ready(_config);
        }

        // Danh mục và chi tiết sản phẩm.
        public async Task<IActionResult> Index(string? keyword, int? brand, decimal? min, decimal? max, string sort = "new", int page = 1)
        {
            await GetData();
            keyword = keyword?.Trim();
            min = min is >= 0 ? min : null;
            max = max is >= 0 ? max : null;
            if (min > max)
                (min, max) = (max, min);
            var q = _context.Sanphams.Include(x => x.MaKmNavigation).Include(x => x.MaLspNavigation).Where(x => x.TrangThai == 1);
            if (!string.IsNullOrWhiteSpace(keyword))
                q = q.Where(x => x.TenSp.Contains(keyword));
            if (brand.HasValue)
                q = q.Where(x => x.MaLsp == brand);
            // Price is recalculated from the current promotion, never a stale Session price.
            var products = await q.AsNoTracking().ToListAsync();
            var now = ShopRules.Now;
            if (min.HasValue)
                products = products.Where(x => ShopRules.Price(x, now) >= min).ToList();
            if (max.HasValue)
                products = products.Where(x => ShopRules.Price(x, now) <= max).ToList();
            products = sort switch
            {
                "price-asc" => products.OrderBy(x => ShopRules.Price(x, now)).ToList(),
                "price-desc" => products.OrderByDescending(x => ShopRules.Price(x, now)).ToList(),
                "rating" => products.OrderByDescending(x => x.SoSao).ThenByDescending(x => x.SoDanhGia).ToList(),
                _ => products.OrderByDescending(x => x.MaSp).ToList()
            };
            var total = products.Count;
            page = Math.Clamp(page, 1, Math.Max(1, (int)Math.Ceiling(total / 12m)));
            return View(new CatalogueViewModel { Items = products.Skip((page - 1) * 12).Take(12).ToList(), Keyword = keyword, Brand = brand, Min = min, Max = max, Sort = sort, Page = page, Total = total });
        }
        public async Task<IActionResult> List(int id)
        {
            await GetData();
            return RedirectToAction(nameof(Index), new
            {
                brand = id
            });
        }
        public async Task<IActionResult> Details(int id)
        {
            await GetData();
            var p = await _context.Sanphams.Include(x => x.MaKmNavigation).Include(x => x.MaLspNavigation).FirstOrDefaultAsync(x => x.MaSp == id && x.TrangThai == 1);
            if (p == null)
                return NotFound();
            ViewBag.Reviews = await _context.Danhgia.Include(x => x.MaNdNavigation).Where(x => x.MaSp == id).OrderByDescending(x => x.NgayLap).ToListAsync();
            ViewBag.CanReview = CurrentUser != null && await _context.Chitiethoadons.AnyAsync(x => x.MaSp == id && x.MaHdNavigation.MaNd == CurrentUser.MaNd && x.MaHdNavigation.TrangThai == "Đã giao hàng" && x.MaHdNavigation.TrangThaiThanhToan == ShopRules.Paid);
            ViewBag.MyReview = CurrentUser == null ? null : await _context.Danhgia.FirstOrDefaultAsync(x => x.MaSp == id && x.MaNd == CurrentUser.MaNd);
            ViewBag.Related = await _context.Sanphams.Include(x => x.MaKmNavigation).Where(x => x.MaLsp == p.MaLsp && x.MaSp != id && x.TrangThai == 1).Take(4).ToListAsync();
            return View(p);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Review(int id, int stars, string? comment)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            if (stars < 1 || stars > 5 || string.IsNullOrWhiteSpace(comment) || comment.Trim().Length > 255)
            {
                TempData["Error"] = "Chọn từ 1–5 sao và nhập nhận xét tối đa 255 ký tự.";
                return RedirectToAction(nameof(Details), new
                {
                    id
                });
            }
            var can = await _context.Chitiethoadons.AnyAsync(x => x.MaSp == id && x.MaHdNavigation.MaNd == CurrentUser.MaNd && x.MaHdNavigation.TrangThai == "Đã giao hàng" && x.MaHdNavigation.TrangThaiThanhToan == ShopRules.Paid);
            if (!can)
                return Forbid();
            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var r = await _context.Danhgia.FirstOrDefaultAsync(x => x.MaSp == id && x.MaNd == CurrentUser.MaNd);
            if (r == null)
            {
                r = new Danhgium { MaSp = id, MaNd = CurrentUser.MaNd };
                _context.Add(r);
            }
            r.SoSao = stars;
            r.BinhLuan = comment.Trim();
            r.NgayLap = ShopRules.Now;
            await _context.SaveChangesAsync();
            var p = await _context.Sanphams.FindAsync(id);
            if (p != null)
            {
                var starsAll = await _context.Danhgia.Where(x => x.MaSp == id).Select(x => x.SoSao).ToListAsync();
                p.SoDanhGia = starsAll.Count;
                p.SoSao = (int)Math.Round(starsAll.Average(), MidpointRounding.AwayFromZero);
                await _context.SaveChangesAsync();
            }
            await tx.CommitAsync();
            TempData["Success"] = "Cảm ơn bạn đã chia sẻ trải nghiệm!";
            return RedirectToAction(nameof(Details), new
            {
                id
            });
        }
        public async Task<IActionResult> Policy()
        {
            await GetData();
            return View("~/Views/Home/Privacy.cshtml");
        }
        public async Task<IActionResult> AccessDenied()
        {
            await GetData();
            Response.StatusCode = 403;
            return View();
        }

        // Đăng nhập, đăng ký và hồ sơ khách hàng.
        public async Task<IActionResult> Login(string? returnUrl, int? googleError)
        {
            await GetData();
            if (googleError.HasValue)
                TempData["Error"] = "Google chưa xác thực thành công. Bạn có thể thử lại hoặc dùng tài khoản thường.";
            return View(new LoginForm { ReturnUrl = returnUrl });
        }
        [HttpPost, EnableRateLimiting("Account")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginForm form)
        {
            await GetData();
            if (!ModelState.IsValid)
                return View(form);
            var key = form.TaiKhoan.Trim();
            var u = await _context.Nguoidungs.Include(x => x.MaQuyenNavigation).FirstOrDefaultAsync(x => x.TaiKhoan == key || x.Email == key);
            var valid = false;
            if (u != null && u.CoMatKhau && u.TrangThai == 1)
            {
                // Upgrade imported MD5 only after the original password has been proved.
                if (u.MatKhau.Length == 32 && u.MatKhau.All(Uri.IsHexDigit))
                {
                    valid = ShopRules.SameSignature(u.MatKhau, Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(form.MatKhau))));
                    if (valid)
                    {
                        u.MatKhau = _hasher.HashPassword(u, form.MatKhau);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    try
                    {
                        var result = _hasher.VerifyHashedPassword(u, u.MatKhau, form.MatKhau);
                        valid = result != PasswordVerificationResult.Failed;
                        if (result == PasswordVerificationResult.SuccessRehashNeeded)
                        {
                            u.MatKhau = _hasher.HashPassword(u, form.MatKhau);
                            await _context.SaveChangesAsync();
                        }
                    }
                    catch (FormatException) { valid = false; }
                }
            }
            if (!valid || u == null)
            {
                ModelState.AddModelError("", "Tài khoản hoặc mật khẩu chưa đúng, hoặc tài khoản đã bị khóa.");
                return View(form);
            }
            await SignIn(u);
            return !string.IsNullOrEmpty(form.ReturnUrl) && Url.IsLocalUrl(form.ReturnUrl) ? LocalRedirect(form.ReturnUrl) : RedirectToAction(u.MaQuyenNavigation.TenQuyen == "Admin" ? "Index" : "Index", u.MaQuyenNavigation.TenQuyen == "Admin" ? "Admin" : "Customer");
        }
        public async Task<IActionResult> Register()
        {
            await GetData();
            return View(new RegisterForm());
        }
        [HttpPost, EnableRateLimiting("Account")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterForm f)
        {
            await GetData();
            f.Email = f.Email?.Trim().ToLowerInvariant() ?? "";
            f.TaiKhoan = f.TaiKhoan?.Trim() ?? "";
            if (await _context.Nguoidungs.AnyAsync(x => x.Email == f.Email || x.TaiKhoan == f.TaiKhoan))
                ModelState.AddModelError("", "Email hoặc tên tài khoản đã được sử dụng.");
            if (!ModelState.IsValid)
                return View(f);
            var role = await _context.Phanquyens.SingleAsync(x => x.TenQuyen == "Customer");
            var rank = await _context.Hangkhachhangs.OrderBy(x => x.NguongChiTieu).FirstAsync();
            var u = new Nguoidung { Ho = f.Ho.Trim(), Ten = f.Ten.Trim(), TaiKhoan = f.TaiKhoan, Email = f.Email, Sdt = f.Sdt, DiaChi = f.DiaChi.Trim(), GioiTinh = f.GioiTinh, MaQuyen = role.MaQuyen, MaHang = rank.MaHang, TrangThai = 1, CoMatKhau = true };
            u.MatKhau = _hasher.HashPassword(u, f.MatKhau);
            try
            {
                _context.Add(u);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException) { ModelState.AddModelError("", "Thông tin tài khoản bị trùng. Vui lòng thử tên hoặc email khác."); return View(f); }
            TempData["Success"] = "Đăng ký thành công! Bạn có thể đăng nhập ngay.";
            return RedirectToAction(nameof(Login));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signout()
        {
            await GetData();
            await HttpContext.SignOutAsync("Shop");
            await HttpContext.SignOutAsync("External");
            HttpContext.Session.Clear();
            return RedirectToAction(nameof(Index));
        }
        private async Task SignIn(Nguoidung u)
        {
            // Retain only the guest cart; never merge a previous signed-in customer's cart.
            var guest = CurrentUser == null ? GetCartItems() : new List<CartItem>();
            var saved = await _context.Giohangs.Where(x => x.MaNd == u.MaNd).ToListAsync();
            var merged = saved.Select(x => new CartItem { MaSp = x.MaSp, SoLuong = x.SoLuong }).ToList();
            foreach (var c in guest)
            {
                var old = merged.FirstOrDefault(x => x.MaSp == c.MaSp);
                if (old == null)
                    merged.Add(c);
                else
                    old.SoLuong = Math.Min(999, Math.Max(old.SoLuong, c.SoLuong));
            }
            var ids = merged.Select(x => x.MaSp).ToList();
            var allowed = await _context.Sanphams.Where(x => ids.Contains(x.MaSp) && x.TrangThai == 1).Select(x => x.MaSp).ToListAsync();
            merged = merged.Where(x => allowed.Contains(x.MaSp)).ToList();
            HttpContext.Session.Clear();
            CurrentUser = u;
            await SyncCart(merged);
            var claims = new[] { new Claim(ClaimTypes.NameIdentifier, u.MaNd.ToString()), new Claim(ClaimTypes.Name, u.TaiKhoan) };
            await HttpContext.SignInAsync("Shop", new ClaimsPrincipal(new ClaimsIdentity(claims, "Shop")));
        }
        public async Task<IActionResult> CustomerInfo()
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            return View(new ProfileForm { Ho = CurrentUser.Ho, Ten = CurrentUser.Ten, Sdt = CurrentUser.Sdt, DiaChi = CurrentUser.DiaChi, GioiTinh = CurrentUser.GioiTinh });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CustomerInfo(ProfileForm f)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            if (!ModelState.IsValid)
                return View(f);
            CurrentUser.Ho = f.Ho.Trim();
            CurrentUser.Ten = f.Ten.Trim();
            CurrentUser.Sdt = f.Sdt;
            CurrentUser.DiaChi = f.DiaChi.Trim();
            CurrentUser.GioiTinh = f.GioiTinh;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật hồ sơ.";
            return RedirectToAction(nameof(CustomerInfo));
        }
        public async Task<IActionResult> ChangePassword()
        {
            await GetData();
            return CurrentUser == null ? LoginRedirect() : View(new ChangePasswordForm());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordForm f)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            if (CurrentUser.CoMatKhau)
            {
                var ok = false;
                if (!string.IsNullOrEmpty(f.MatKhauCu))
                {
                    if (CurrentUser.MatKhau.Length == 32 && CurrentUser.MatKhau.All(Uri.IsHexDigit))
                        ok = ShopRules.SameSignature(CurrentUser.MatKhau, Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(f.MatKhauCu))));
                    else
                        try
                        {
                            ok = _hasher.VerifyHashedPassword(CurrentUser, CurrentUser.MatKhau, f.MatKhauCu) != PasswordVerificationResult.Failed;
                        }
                        catch (FormatException) { }
                }
                if (!ok)
                    ModelState.AddModelError("MatKhauCu", "Mật khẩu hiện tại chưa đúng.");
            }
            if (!ModelState.IsValid)
                return View(f);
            CurrentUser.MatKhau = _hasher.HashPassword(CurrentUser, f.MatKhauMoi);
            CurrentUser.CoMatKhau = true;
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cập nhật mật khẩu.";
            return RedirectToAction(nameof(CustomerInfo));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Google(bool link = false)
        {
            await GetData();
            if (!(ViewBag.GoogleEnabled is true))
            {
                TempData["Error"] = "Đăng nhập Google sẽ hoạt động sau khi chủ shop cấu hình Google OAuth.";
                return RedirectToAction(nameof(Login));
            }
            var props = new AuthenticationProperties { RedirectUri = Url.Action(nameof(GoogleCallback)) };
            if (link)
            {
                if (CurrentUser == null)
                    return LoginRedirect();
                props.Items["LinkUserId"] = CurrentUser.MaNd.ToString();
            }
            return Challenge(props, "Google");
        }
        public async Task<IActionResult> GoogleCallback()
        {
            await GetData();
            var auth = await HttpContext.AuthenticateAsync("External");
            await HttpContext.SignOutAsync("External");
            var id = auth.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var email = auth.Principal?.FindFirstValue(ClaimTypes.Email)?.Trim().ToLowerInvariant();
            if (!auth.Succeeded || string.IsNullOrEmpty(id) || string.IsNullOrEmpty(email) || auth.Principal?.FindFirstValue("google:verified") != "true")
            {
                TempData["Error"] = "Không xác minh được tài khoản Google.";
                return RedirectToAction(nameof(Login));
            }
            var u = await _context.Nguoidungs.FirstOrDefaultAsync(x => x.GoogleId == id);
            if (auth.Properties?.Items.TryGetValue("LinkUserId", out var linkId) == true)
            {
                if (CurrentUser == null || CurrentUser.MaNd.ToString() != linkId || !string.Equals(CurrentUser.Email, email, StringComparison.OrdinalIgnoreCase) || (u != null && u.MaNd != CurrentUser.MaNd))
                    return Forbid();
                CurrentUser.GoogleId = id;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Đã liên kết tài khoản Google.";
                return RedirectToAction(nameof(CustomerInfo));
            }
            if (u != null)
            {
                if (u.TrangThai != 1)
                    return Forbid();
                await SignIn(u);
                return RedirectToAction(nameof(Index));
            }
            if (await _context.Nguoidungs.AnyAsync(x => x.Email == email))
            {
                TempData["Error"] = "Email đã có tài khoản. Hãy đăng nhập bằng mật khẩu, vào Hồ sơ và chọn Liên kết Google.";
                return RedirectToAction(nameof(Login));
            }
            var pending = new GooglePending { Id = id, Email = email, Ho = auth.Principal?.FindFirstValue(ClaimTypes.Surname) ?? "", Ten = auth.Principal?.FindFirstValue(ClaimTypes.GivenName) ?? "", Expires = DateTime.UtcNow.AddMinutes(10) };
            HttpContext.Session.SetString("GooglePending", JsonSerializer.Serialize(pending));
            return RedirectToAction(nameof(CompleteGoogle));
        }
        private GooglePending? ReadGooglePending()
        {
            try
            {
                var p = JsonSerializer.Deserialize<GooglePending>(HttpContext.Session.GetString("GooglePending") ?? "null");
                return p?.Expires > DateTime.UtcNow ? p : null;
            }
            catch (JsonException) { return null; }
        }
        public async Task<IActionResult> CompleteGoogle()
        {
            await GetData();
            var p = ReadGooglePending();
            if (p == null)
                return RedirectToAction(nameof(Login));
            ViewBag.GoogleEmail = p.Email;
            return View(new ProfileForm { Ho = p.Ho.Length <= 20 ? p.Ho : "", Ten = p.Ten.Length <= 20 ? p.Ten : "" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteGoogle(ProfileForm f)
        {
            await GetData();
            var p = ReadGooglePending();
            if (p == null)
                return RedirectToAction(nameof(Login));
            ViewBag.GoogleEmail = p.Email;
            if (!ModelState.IsValid)
                return View(f);
            if (await _context.Nguoidungs.AnyAsync(x => x.Email == p.Email || x.GoogleId == p.Id))
            {
                TempData["Error"] = "Tài khoản đã tồn tại. Vui lòng đăng nhập lại.";
                return RedirectToAction(nameof(Login));
            }
            var role = await _context.Phanquyens.SingleAsync(x => x.TenQuyen == "Customer");
            var rank = await _context.Hangkhachhangs.OrderBy(x => x.NguongChiTieu).FirstAsync();
            var u = new Nguoidung { Ho = f.Ho.Trim(), Ten = f.Ten.Trim(), Email = p.Email, TaiKhoan = "google_" + Guid.NewGuid().ToString("N"), GoogleId = p.Id, CoMatKhau = false, Sdt = f.Sdt, DiaChi = f.DiaChi.Trim(), GioiTinh = f.GioiTinh, MaQuyen = role.MaQuyen, MaHang = rank.MaHang, TrangThai = 1 };
            u.MatKhau = _hasher.HashPassword(u, Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)));
            await using (var tx = await _context.Database.BeginTransactionAsync())
            {
                _context.Add(u);
                await _context.SaveChangesAsync();
                ShopEmail.Welcome(_context, u);
                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            HttpContext.Session.Remove("GooglePending");
            await SignIn(u);
            TempData["Success"] = "Chào mừng bạn đến với ChienNguyenShop!";
            return RedirectToAction(nameof(Index));
        }

        // Giỏ hàng, đặt hàng và lịch sử đơn.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddToCart(int id, int quantity = 1)
        {
            await GetData();
            var p = await _context.Sanphams.FirstOrDefaultAsync(x => x.MaSp == id && x.TrangThai == 1);
            var cart = GetCartItems();
            var item = cart.FirstOrDefault(x => x.MaSp == id);
            if (p == null || quantity < 1 || quantity > 999 || (item?.SoLuong ?? 0) + quantity > p.SoLuong || (item?.SoLuong ?? 0) + quantity > 999)
            {
                TempData["Error"] = "Sản phẩm không còn bán hoặc số lượng vượt tồn kho.";
                return RedirectToAction(nameof(ViewCart));
            }
            if (item == null)
                cart.Add(new CartItem { MaSp = id, SoLuong = quantity });
            else
                item.SoLuong += quantity;
            await SyncCart(cart);
            TempData["Success"] = "Đã thêm sản phẩm vào giỏ hàng.";
            return RedirectToAction(nameof(ViewCart));
        }
        public async Task<IActionResult> ViewCart()
        {
            await GetData();
            return View(await ReadCart());
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateItem(int id, int quantity)
        {
            await GetData();
            var p = await _context.Sanphams.FirstOrDefaultAsync(x => x.MaSp == id && x.TrangThai == 1);
            if (p == null || quantity < 1 || quantity > 999 || quantity > p.SoLuong)
                TempData["Error"] = "Số lượng phải từ 1 đến số lượng tồn kho (tối đa 999).";
            else
            {
                var cart = GetCartItems();
                var item = cart.FirstOrDefault(x => x.MaSp == id);
                if (item != null)
                    item.SoLuong = quantity;
                await SyncCart(cart);
            }
            return RedirectToAction(nameof(ViewCart));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveItem(int id)
        {
            await GetData();
            var cart = GetCartItems();
            cart.RemoveAll(x => x.MaSp == id);
            await SyncCart(cart);
            return RedirectToAction(nameof(ViewCart));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ClearCart()
        {
            await GetData();
            await SyncCart(new());
            return RedirectToAction(nameof(ViewCart));
        }
        public async Task<IActionResult> CheckOut()
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            var cart = await ReadCart();
            if (cart.Items.Count == 0)
                return RedirectToAction(nameof(ViewCart));
            var token = HttpContext.Session.GetString("CheckoutToken") ?? Guid.NewGuid().ToString();
            HttpContext.Session.SetString("CheckoutToken", token);
            ViewBag.Cart = cart;
            return View(new CheckoutForm { Token = token, NguoiNhan = CurrentUser.Ho + " " + CurrentUser.Ten, Sdt = CurrentUser.Sdt, DiaChi = CurrentUser.DiaChi.Length <= 100 ? CurrentUser.DiaChi : "" });
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBill(CheckoutForm f)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            if (!Guid.TryParse(f.Token, out _))
                return BadRequest();
            var existing = await _context.Hoadons.FirstOrDefaultAsync(x => x.MaDatHang == f.Token && x.MaNd == CurrentUser.MaNd);
            if (existing != null)
                return RedirectToAction(nameof(OrderDetails), new
                {
                    id = existing.MaHd
                });
            if (f.Token != HttpContext.Session.GetString("CheckoutToken"))
                return BadRequest("Phiên thanh toán đã hết hạn. Hãy mở lại giỏ hàng.");
            if (f.PhuongThucTt == "MoMo" && !MomoClient.Ready(_config))
                ModelState.AddModelError("", "MoMo chưa được cấu hình. Vui lòng chọn thanh toán trực tiếp.");
            var viewCart = await ReadCart();
            if (viewCart.Items.Count == 0 || viewCart.Errors.Count > 0)
                ModelState.AddModelError("", "Giỏ hàng rỗng hoặc có sản phẩm không đủ số lượng.");
            if (!ModelState.IsValid)
            {
                ViewBag.Cart = viewCart;
                return View("CheckOut", f);
            }
            Hoadon hd;
            await using (var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable))
            {
                try
                {
                    var cart = GetCartItems();
                    var ids = cart.Select(x => x.MaSp).ToList();
                    var products = await _context.Sanphams.Include(x => x.MaKmNavigation).Where(x => ids.Contains(x.MaSp)).OrderBy(x => x.MaSp).ToListAsync();
                    var user = await _context.Nguoidungs.AsNoTracking().Include(x => x.MaHangNavigation).FirstAsync(x => x.MaNd == CurrentUser.MaNd && x.TrangThai == 1);
                    if (products.Count != cart.Count || products.Any(x => x.TrangThai != 1 || x.SoLuong < cart.First(i => i.MaSp == x.MaSp).SoLuong))
                        throw new InvalidOperationException("Tồn kho vừa thay đổi. Vui lòng kiểm tra lại giỏ hàng.");
                    var now = ShopRules.Now;
                    var total = products.Sum(p => ShopRules.Price(p, now) * cart.First(x => x.MaSp == p.MaSp).SoLuong);
                    var final = total - ShopRules.Discount(total, user.MaHangNavigation.PhanTramGiam);
                    if (total > 9999999999999999m)
                        throw new InvalidOperationException("Tổng đơn hàng vượt giới hạn cho phép.");
                    if (f.PhuongThucTt == "MoMo" && (final < 1000 || final > 50000000 || decimal.Truncate(final) != final))
                        throw new InvalidOperationException("MoMo Sandbox hỗ trợ số tiền từ 1.000đ đến 50.000.000đ và không có phần lẻ. Hãy chọn thanh toán trực tiếp.");
                    hd = new Hoadon { MaNd = user.MaNd, NgayLap = now, NguoiNhan = f.NguoiNhan.Trim(), Sdt = f.Sdt, DiaChi = f.DiaChi.Trim(), PhuongThucTt = f.PhuongThucTt, TrangThai = "Chờ xử lý", TrangThaiThanhToan = ShopRules.Unpaid, TongTien = total, MaHang = user.MaHang, PhanTramGiam = user.MaHangNavigation.PhanTramGiam, MaDatHang = f.Token };
                    _context.Add(hd);
                    await _context.SaveChangesAsync();
                    foreach (var p in products)
                    {
                        var n = cart.First(x => x.MaSp == p.MaSp).SoLuong;
                        var price = ShopRules.Price(p, now);
                        var changed = await _context.Sanphams.Where(x => x.MaSp == p.MaSp && x.TrangThai == 1 && x.SoLuong >= n).ExecuteUpdateAsync(s => s.SetProperty(x => x.SoLuong, x => x.SoLuong - n));
                        if (changed != 1)
                            throw new InvalidOperationException("Sản phẩm vừa hết hàng. Vui lòng kiểm tra lại giỏ.");
                        _context.Chitiethoadons.Add(new Chitiethoadon { MaHd = hd.MaHd, MaSp = p.MaSp, SoLuong = n, DonGia = price, ThanhTien = price * n });
                    }
                    _context.Giohangs.RemoveRange(await _context.Giohangs.Where(x => x.MaNd == user.MaNd).ToListAsync());
                    var emailLines = products.Select(p => new CartLine { SanPham = p, SoLuong = cart.First(x => x.MaSp == p.MaSp).SoLuong, DonGia = ShopRules.Price(p, now) }).ToList();
                    ShopEmail.Order(_context, user, hd, emailLines, final);
                    await _context.SaveChangesAsync();
                    await tx.CommitAsync();
                }
                catch (Exception ex) when (ex is InvalidOperationException or DbUpdateException or System.Data.Common.DbException)
                {
                    await tx.RollbackAsync();
                    _context.ChangeTracker.Clear();
                    var old = await _context.Hoadons.FirstOrDefaultAsync(x => x.MaDatHang == f.Token && x.MaNd == CurrentUser.MaNd);
                    if (old != null)
                        return RedirectToAction(nameof(OrderDetails), new
                        {
                            id = old.MaHd
                        });
                    ModelState.AddModelError("", ex is InvalidOperationException ? ex.Message : "Chưa lưu được đơn hàng. Hãy thử lại sau khi kiểm tra giỏ.");
                    ViewBag.Cart = await ReadCart();
                    return View("CheckOut", f);
                }
            }
            HttpContext.Session.Remove("shopcart");
            HttpContext.Session.Remove("CheckoutToken");
            TempData["Success"] = "Đặt hàng thành công! Cảm ơn bạn đã tin tưởng ChienNguyenShop.";
            return RedirectToAction(nameof(OrderDetails), new
            {
                id = hd.MaHd
            });
        }
        public async Task<IActionResult> Orders()
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            return View(await _context.Hoadons.Where(x => x.MaNd == CurrentUser.MaNd).OrderByDescending(x => x.MaHd).ToListAsync());
        }
        public async Task<IActionResult> OrderDetails(int id)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            var hd = await _context.Hoadons.Include(x => x.Chitiethoadons).ThenInclude(x => x.MaSpNavigation).FirstOrDefaultAsync(x => x.MaHd == id && x.MaNd == CurrentUser.MaNd);
            if (hd == null)
                return NotFound();
            ViewBag.Payments = await _context.Giaodichthanhtoans.Where(x => x.MaHd == id).OrderByDescending(x => x.MaGd).ToListAsync();
            return View(hd);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int id)
        {
            await GetData();
            if (CurrentUser == null)
                return LoginRedirect();
            await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable);
            var hd = await _context.Hoadons.Include(x => x.Chitiethoadons).FirstOrDefaultAsync(x => x.MaHd == id && x.MaNd == CurrentUser.MaNd);
            if (hd == null)
                return NotFound();
            if (hd.TrangThai != "Chờ xử lý" || hd.TrangThaiThanhToan == ShopRules.Paid || await _context.Giaodichthanhtoans.AnyAsync(x => x.MaHd == id && x.TrangThai != "Thất bại"))
            {
                TempData["Error"] = "Đơn đã được xử lý, đã thanh toán hoặc có giao dịch MoMo đang chờ đối soát. Vui lòng liên hệ shop.";
                return RedirectToAction(nameof(OrderDetails), new
                {
                    id
                });
            }
            hd.TrangThai = "Đã hủy";
            foreach (var ct in hd.Chitiethoadons.OrderBy(x => x.MaSp))
                await _context.Sanphams.Where(x => x.MaSp == ct.MaSp).ExecuteUpdateAsync(s => s.SetProperty(x => x.SoLuong, x => x.SoLuong + ct.SoLuong));
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
            TempData["Success"] = "Đã hủy đơn và hoàn lại số lượng tồn kho.";
            return RedirectToAction(nameof(OrderDetails), new
            {
                id
            });
        }

        // Các phương thức hỗ trợ dùng trong CustomerController.
        private IActionResult LoginRedirect() => RedirectToAction("Login", "Customer", new { returnUrl = Request.Path.ToString() + Request.QueryString });
        private List<CartItem> GetCartItems()
        {
            try
            {
                return JsonSerializer.Deserialize<List<CartItem>>(HttpContext.Session.GetString("shopcart") ?? "[]")?.Where(x => x.SoLuong > 0 && x.SoLuong <= 999).GroupBy(x => x.MaSp).Select(g => g.First()).Take(100).ToList() ?? new();
            }
            catch (JsonException) { return new(); }
        }
        private void SaveCartSession(List<CartItem> cart) => HttpContext.Session.SetString("shopcart", JsonSerializer.Serialize(cart));
        private async Task SyncCart(List<CartItem> cart)
        {
            SaveCartSession(cart);
            if (CurrentUser == null)
                return;
            HttpContext.Session.SetString("cartOwner", CurrentUser.MaNd.ToString());
            await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
            var old = await _context.Giohangs.Where(x => x.MaNd == CurrentUser.MaNd).ToListAsync();
            _context.Giohangs.RemoveRange(old);
            await _context.SaveChangesAsync();
            _context.Giohangs.AddRange(cart.Select(x => new Giohang { MaNd = CurrentUser.MaNd, MaSp = x.MaSp, SoLuong = x.SoLuong }));
            await _context.SaveChangesAsync();
            await tx.CommitAsync();
        }
        private async Task<CartViewModel> ReadCart()
        {
            var cart = GetCartItems();
            var ids = cart.Select(x => x.MaSp).ToList();
            var products = await _context.Sanphams.AsNoTracking().Include(x => x.MaKmNavigation).Where(x => ids.Contains(x.MaSp)).ToListAsync();
            var vm = new CartViewModel();
            foreach (var item in cart)
            {
                var p = products.FirstOrDefault(x => x.MaSp == item.MaSp);
                if (p == null)
                {
                    vm.Errors.Add("Một sản phẩm trong giỏ không còn tồn tại. Vui lòng xóa giỏ và chọn lại.");
                    continue;
                }
                vm.Items.Add(new CartLine { SanPham = p, SoLuong = item.SoLuong, DonGia = ShopRules.Price(p, ShopRules.Now) });
                if (p.TrangThai != 1 || p.SoLuong < item.SoLuong)
                    vm.Errors.Add(p.TenSp + ": số lượng hiện không đủ hoặc đã ngừng bán.");
            }
            return vm;
        }
    }
}
