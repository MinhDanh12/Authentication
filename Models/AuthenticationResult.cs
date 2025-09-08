namespace AuthenticationModule.Models
{
    public class AuthenticationResult
    {
        public bool IsSuccess { get; set; }
        public string? Token { get; set; }
        public string? RefreshToken { get; set; }
        public string? ErrorMessage { get; set; }
        public ApplicationUser? User { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
