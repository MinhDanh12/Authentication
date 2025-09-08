using AuthenticationModule.Models;

namespace AuthenticationModule.Services
{
    public interface IAuthenticationService
    {
        Task<AuthenticationResult> LoginAsync(string emailOrUsername, string password, bool rememberMe = false);
        Task<AuthenticationResult> RegisterAsync(RegisterViewModel model);
        Task<bool> LogoutAsync(string userId);
        Task<AuthenticationResult> RefreshTokenAsync(string refreshToken);
        Task<bool> ValidateUserAsync(string emailOrUsername, string password);
        Task<ApplicationUser?> GetUserByEmailOrUsernameAsync(string emailOrUsername);
        Task<bool> IsUserActiveAsync(string userId);
    }
}
