using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using AuthenticationModule.Models;

namespace AuthenticationModule.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            var user = HttpContext.User;
            ViewBag.UserName = user.Identity?.Name;
            ViewBag.UserType = user.FindFirst("UserType")?.Value;
            ViewBag.FullName = $"{user.FindFirst("FirstName")?.Value} {user.FindFirst("LastName")?.Value}";
            
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
