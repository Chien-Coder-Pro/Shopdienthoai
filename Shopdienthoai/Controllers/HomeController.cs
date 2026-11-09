using Microsoft.AspNetCore.Mvc;
using Shopdienthoai.Models;
using System.Diagnostics;
namespace Shopdienthoai.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index() => RedirectToAction("Index", "Customer");
        public IActionResult Privacy() => RedirectToAction("Policy", "Customer");
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
