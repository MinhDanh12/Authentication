using AuthenticationModule.Models;

namespace AuthenticationModule.Services
{
    public interface IJwtService
    {
        string GenerateJwtToken(ApplicationUser user);
        string GenerateRefreshToken();
        bool ValidateToken(string token);
        string? GetUserIdFromToken(string token);
        DateTime GetTokenExpiration(string token);
    }
}
