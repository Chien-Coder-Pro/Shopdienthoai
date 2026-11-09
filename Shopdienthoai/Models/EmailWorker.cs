using Shopdienthoai.Data;
using System.Net;
using System.Net.Mail;
using Microsoft.EntityFrameworkCore;

namespace Shopdienthoai.Models
{
    public class EmailWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopes; private readonly IConfiguration _config; private readonly ILogger<EmailWorker> _logger;
        public EmailWorker(IServiceScopeFactory scopes, IConfiguration config, ILogger<EmailWorker> logger)
        {
            _scopes = scopes;
            _config = config;
            _logger = logger;
        }
        protected override async Task ExecuteAsync(CancellationToken stop)
        {
            while (!stop.IsCancellationRequested)
            {
                if (_config.GetValue<bool>("Email:Enabled"))
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<ShopDbContext>();
                        var now = ShopRules.Now;
                        var emails = await db.Emailhangdois.Where(x => x.DaGuiLuc == null && x.SoLanThu < 5 && (x.ThuTiepLuc == null || x.ThuTiepLuc <= now)).OrderBy(x => x.MaEmail).Take(10).ToListAsync(stop);
                        foreach (var e in emails)
                        {
                            try
                            {
                                using var smtp = new SmtpClient(_config["Email:Host"], _config.GetValue<int>("Email:Port", 587)) { EnableSsl = true, UseDefaultCredentials = false, Credentials = new NetworkCredential(_config["Email:UserName"], _config["Email:Password"]), Timeout = 20000 };
                                using var mail = new MailMessage { From = new MailAddress(_config["Email:FromAddress"]!, _config["Email:FromName"]), Subject = e.TieuDe, Body = e.NoiDung, IsBodyHtml = true };
                                mail.To.Add(e.NguoiNhan);
                                await smtp.SendMailAsync(mail, stop);
                                e.DaGuiLuc = ShopRules.Now;
                                e.LoiGanNhat = null;
                            }
                            catch (Exception ex) when (ex is SmtpException or InvalidOperationException or ArgumentException)
                            {
                                e.LoiGanNhat = "Chưa gửi được. Kiểm tra cấu hình SMTP và tài khoản gửi.";
                                e.ThuTiepLuc = ShopRules.Now.AddMinutes(Math.Pow(2, e.SoLanThu + 1));
                            }
                            e.SoLanThu++;
                            await db.SaveChangesAsync(stop);
                        }
                    }
                    catch (Exception ex) when (!stop.IsCancellationRequested) { _logger.LogWarning("Email worker chưa thể xử lý hàng đợi ({Type}).", ex.GetType().Name); }
                }
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(30), stop);
                }
                catch (OperationCanceledException) { break; }
            }
        }
    }
}
