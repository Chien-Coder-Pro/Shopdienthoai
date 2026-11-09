using Shopdienthoai.Data;
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    public static class ShopEmail
    {
        public static string E(string? x) => WebUtility.HtmlEncode(x ?? "");
        private static string Wrap(string body) => "<div style='font-family:Arial,sans-serif;max-width:650px;margin:auto;color:#172b21'><h1 style='color:#16a34a'>ChienNguyenShop</h1>" + body + "<hr><p>Long Xuyên, An Giang · 0384229772</p><p>Cảm ơn bạn đã tin tưởng và sử dụng dịch vụ. Chúc bạn một ngày tốt lành!</p></div>";
        public static void Welcome(ShopDbContext db, Nguoidung u) => db.Emailhangdois.Add(new Emailhangdoi { Khoa = "welcome-google-" + u.MaNd, NguoiNhan = u.Email, TieuDe = "Chào mừng bạn đến ChienNguyenShop!", NoiDung = Wrap("<h2>Xin chào " + E(u.Ho + " " + u.Ten) + "!</h2><p>Bạn đã đăng ký tài khoản bằng Google thành công. Rất vui được đồng hành cùng bạn.</p>") });
        public static void Order(ShopDbContext db, Nguoidung u, Hoadon hd, List<CartLine> items, decimal final)
        {
            var lines = string.Join("", items.Select(i => "<tr><td>" + E(i.SanPham.TenSp) + "</td><td>" + i.SoLuong + "</td><td>" + ShopRules.Money(i.DonGia) + "</td><td>" + ShopRules.Money(i.ThanhTien) + "</td></tr>"));
            var content = $"<h2>Đã tiếp nhận đơn hàng #{hd.MaHd}</h2><p>Xin chào {E(u.Ho + " " + u.Ten)}, cảm ơn bạn đã lựa chọn shop!</p><table cellpadding='10' border='1' style='border-collapse:collapse;width:100%'><tr><th>Sản phẩm</th><th>SL</th><th>Đơn giá</th><th>Thành tiền</th></tr>{lines}</table><p>Tiền hàng: {ShopRules.Money(hd.TongTien)}<br>Ưu đãi thành viên: {hd.PhanTramGiam}%<br><strong>Tổng thanh toán: {ShopRules.Money(final)}</strong></p><p>Người nhận: {E(hd.NguoiNhan)}<br>Điện thoại: {E(hd.Sdt)}<br>Địa chỉ: {E(hd.DiaChi)}</p><p>Phương thức: {E(hd.PhuongThucTt)}<br>Trạng thái thanh toán lúc đặt đơn: {E(hd.TrangThaiThanhToan)}<br>Trạng thái đơn: {E(hd.TrangThai)}</p>";
            db.Emailhangdois.Add(new Emailhangdoi { Khoa = "order-" + hd.MaHd, NguoiNhan = u.Email, TieuDe = "ChienNguyenShop – xác nhận đơn hàng #" + hd.MaHd, NoiDung = Wrap(content) });
        }
    }
}
