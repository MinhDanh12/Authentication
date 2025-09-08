using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuthenticationModule.Data;
using AuthenticationModule.Models;

namespace AuthenticationModule.Services
{
    public class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;

        public AuthenticationService(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ApplicationDbContext context,
            IJwtService jwtService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _context = context;
            _jwtService = jwtService;
        }

        public async Task<AuthenticationResult> LoginAsync(string emailOrUsername, string password, bool rememberMe = false)
        {
            try
            {
                var user = await GetUserByEmailOrUsernameAsync(emailOrUsername);
                if (user == null || !user.IsActive)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Tài khoản không tồn tại hoặc đã bị vô hiệu hóa"
                    };
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
                if (!result.Succeeded)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Email/Username hoặc mật khẩu không đúng"
                    };
                }

                // Generate tokens
                var token = _jwtService.GenerateJwtToken(user);
                var refreshToken = _jwtService.GenerateRefreshToken();

                // Create user session
                var session = new UserSession
                {
                    UserId = user.Id,
                    SessionToken = token,
                    RefreshToken = refreshToken,
                    ExpiresAt = rememberMe ? DateTime.UtcNow.AddDays(30) : DateTime.UtcNow.AddHours(1),
                    DeviceInfo = "Web Browser",
                    IpAddress = "127.0.0.1" // This should be retrieved from HttpContext
                };

                _context.UserSessions.Add(session);

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);

                await _context.SaveChangesAsync();

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Token = token,
                    RefreshToken = refreshToken,
                    User = user,
                    ExpiresAt = session.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi đăng nhập: {ex.Message}"
                };
            }
        }

        public async Task<AuthenticationResult> RegisterAsync(RegisterViewModel model)
        {
            try
            {
                // Check if user already exists
                var existingUser = await GetUserByEmailOrUsernameAsync(model.Email);
                if (existingUser != null)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Email hoặc Username đã được sử dụng"
                    };
                }

                var user = new ApplicationUser
                {
                    UserName = model.UserName,
                    Email = model.Email,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    UserType = model.UserType,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = $"Lỗi tạo tài khoản: {errors}"
                    };
                }

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    User = user,
                    ErrorMessage = "Đăng ký thành công"
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi đăng ký: {ex.Message}"
                };
            }
        }

        public async Task<bool> LogoutAsync(string userId)
        {
            try
            {
                // Deactivate all active sessions for the user
                var activeSessions = await _context.UserSessions
                    .Where(s => s.UserId == userId && s.IsActive)
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.IsActive = false;
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<AuthenticationResult> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                var session = await _context.UserSessions
                    .Include(s => s.User)
                    .FirstOrDefaultAsync(s => s.RefreshToken == refreshToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

                if (session == null || session.User == null || !session.User.IsActive)
                {
                    return new AuthenticationResult
                    {
                        IsSuccess = false,
                        ErrorMessage = "Refresh token không hợp lệ hoặc đã hết hạn"
                    };
                }

                // Generate new tokens
                var newToken = _jwtService.GenerateJwtToken(session.User);
                var newRefreshToken = _jwtService.GenerateRefreshToken();

                // Update session
                session.SessionToken = newToken;
                session.RefreshToken = newRefreshToken;
                session.ExpiresAt = DateTime.UtcNow.AddDays(30);

                await _context.SaveChangesAsync();

                return new AuthenticationResult
                {
                    IsSuccess = true,
                    Token = newToken,
                    RefreshToken = newRefreshToken,
                    User = session.User,
                    ExpiresAt = session.ExpiresAt
                };
            }
            catch (Exception ex)
            {
                return new AuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Lỗi refresh token: {ex.Message}"
                };
            }
        }

        public async Task<bool> ValidateUserAsync(string emailOrUsername, string password)
        {
            var user = await GetUserByEmailOrUsernameAsync(emailOrUsername);
            if (user == null || !user.IsActive)
                return false;

            var result = await _signInManager.CheckPasswordSignInAsync(user, password, false);
            return result.Succeeded;
        }

        public async Task<ApplicationUser?> GetUserByEmailOrUsernameAsync(string emailOrUsername)
        {
            return await _userManager.Users
                .FirstOrDefaultAsync(u => u.Email == emailOrUsername || u.UserName == emailOrUsername);
        }

        public async Task<bool> IsUserActiveAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            return user != null && user.IsActive;
        }
    }
}
