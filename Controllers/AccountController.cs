using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthenticationModule.Models;
using AuthenticationModule.Services;
using System.Security.Claims;

namespace AuthenticationModule.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IAuthenticationService _authService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(IAuthenticationService authService, ILogger<AccountController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.LoginAsync(model.EmailOrUsername, model.Password, model.RememberMe);

            if (result.IsSuccess && result.User != null)
            {
                // Create claims for cookie authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, result.User.Id),
                    new Claim(ClaimTypes.Name, result.User.UserName ?? string.Empty),
                    new Claim(ClaimTypes.Email, result.User.Email ?? string.Empty),
                    new Claim("FirstName", result.User.FirstName),
                    new Claim("LastName", result.User.LastName),
                    new Claim("UserType", result.User.UserType.ToString()),
                    new Claim("IsActive", result.User.IsActive.ToString())
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = model.RememberMe,
                    ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(1)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, 
                    new ClaimsPrincipal(claimsIdentity), authProperties);

                _logger.LogInformation("User {UserId} logged in successfully", result.User.Id);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng nhập thất bại");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _authService.RegisterAsync(model);

            if (result.IsSuccess)
            {
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("Login");
            }

            ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng ký thất bại");
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userId))
            {
                await _authService.LogoutAsync(userId);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation("User {UserId} logged out", userId);

            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                ModelState.AddModelError(string.Empty, "Vui lòng nhập email");
                return View();
            }

            var user = await _authService.GetUserByEmailOrUsernameAsync(email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                TempData["InfoMessage"] = "Nếu email tồn tại, bạn sẽ nhận được hướng dẫn đặt lại mật khẩu.";
                return RedirectToAction("Login");
            }

            // TODO: Implement password reset functionality
            TempData["InfoMessage"] = "Chức năng đặt lại mật khẩu sẽ được triển khai trong phiên bản tiếp theo.";
            return RedirectToAction("Login");
        }
    }
}
