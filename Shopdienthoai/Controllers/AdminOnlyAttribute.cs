using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;
using System.Security.Claims;

namespace Shopdienthoai.Controllers
{
    // Kiểm tra quyền bằng dữ liệu hiện tại cho mọi action quản trị.
    // Mỗi Controller vẫn kế thừa trực tiếp Controller như mẫu BTTH9.
    [AttributeUsage(AttributeTargets.Class)]
    public class AdminOnlyAttribute : Attribute, IAsyncActionFilter
    {
        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var http = context.HttpContext;
            var db = http.RequestServices.GetRequiredService<ShopDbContext>();
            Nguoidung? user = null;
            if (int.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            {
                user = await db.Nguoidungs.Include(x => x.MaQuyenNavigation)
                    .Include(x => x.MaHangNavigation)
                    .FirstOrDefaultAsync(x => x.MaNd == id && x.TrangThai == 1);
            }

            if (user == null)
            {
                if (http.User.Identity?.IsAuthenticated == true)
                {
                    await http.SignOutAsync("Shop");
                    http.Session.Clear();
                }
                context.Result = new RedirectToActionResult("Login", "Customer",
                    new
                    {
                        returnUrl = http.Request.Path.ToString() + http.Request.QueryString
                    });
                return;
            }

            if (user.MaQuyenNavigation.TenQuyen != "Admin")
            {
                context.Result = new RedirectToActionResult("AccessDenied", "Customer", null);
                return;
            }

            if (context.Controller is Controller controller)
            {
                controller.ViewBag.CurrentUser = user;
                controller.ViewBag.IsAdmin = true;
                var config = http.RequestServices.GetRequiredService<IConfiguration>();
                controller.ViewBag.GoogleEnabled = config.GetValue<bool>("Authentication:Google:Enabled")
                    && !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientId"])
                    && !string.IsNullOrWhiteSpace(config["Authentication:Google:ClientSecret"]);
                controller.ViewBag.MomoEnabled = MomoClient.Ready(config);
            }
            await next();
        }
    }
}
