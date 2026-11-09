using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Shopdienthoai.Data;
using Shopdienthoai.Models;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));
builder.Services.AddDbContext<ShopDbContext>(o => o.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(o => { o.Cookie.Name = "ChienNguyenShop.Session"; o.Cookie.HttpOnly = true; o.Cookie.IsEssential = true; o.IdleTimeout = TimeSpan.FromHours(2); });
builder.Services.AddScoped<IPasswordHasher<Nguoidung>, PasswordHasher<Nguoidung>>();
var auth = builder.Services.AddAuthentication("Shop")
    .AddCookie("Shop", o => { o.LoginPath = "/Customer/Login"; o.AccessDeniedPath = "/Customer/AccessDenied"; o.Cookie.Name = "ChienNguyenShop.Auth"; o.ExpireTimeSpan = TimeSpan.FromHours(8); o.SlidingExpiration = true; })
    .AddCookie("External", o => { o.Cookie.Name = "ChienNguyenShop.Google"; o.ExpireTimeSpan = TimeSpan.FromMinutes(10); });
if (builder.Configuration.GetValue<bool>("Authentication:Google:Enabled") &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientId"]) &&
    !string.IsNullOrWhiteSpace(builder.Configuration["Authentication:Google:ClientSecret"]))
{
    auth.AddGoogle("Google", o =>
    {
        o.ClientId = builder.Configuration["Authentication:Google:ClientId"]!;
        o.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]!;
        o.SignInScheme = "External";
        o.CallbackPath = "/signin-google";
        o.SaveTokens = false;
        o.Events.OnCreatingTicket = context =>
        {
            var verified = (context.User.TryGetProperty("verified_email", out var a) && a.ValueKind == System.Text.Json.JsonValueKind.True)
                || (context.User.TryGetProperty("email_verified", out var b) && b.ValueKind == System.Text.Json.JsonValueKind.True);
            if (verified)
                context.Identity!.AddClaim(new Claim("google:verified", "true"));
            return Task.CompletedTask;
        };
        o.Events.OnRemoteFailure = context => { context.HandleResponse(); context.Response.Redirect("/Customer/Login?googleError=1"); return Task.CompletedTask; };
    });
}
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddPolicy("Account", context => RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "local", _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.Configure<ForwardedHeadersOptions>(o => o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto);
builder.Services.AddAuthorization();
builder.Services.AddHttpClient<MomoClient>(c => c.Timeout = TimeSpan.FromSeconds(35));
builder.Services.AddHostedService<EmailWorker>();
var app = builder.Build();
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/Home/Error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Customer}/{action=Index}/{id?}");
app.Run();
public partial class Program
{
}

public class ShopAppMarker
{
}
