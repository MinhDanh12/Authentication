using System.ComponentModel.DataAnnotations;

namespace AuthenticationModule.Models
{
    public class UserSession
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        public string SessionToken { get; set; } = string.Empty;

        [Required]
        public string RefreshToken { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public bool IsActive { get; set; } = true;

        public string? DeviceInfo { get; set; }

        public string? IpAddress { get; set; }

        // Navigation property
        public virtual ApplicationUser User { get; set; } = null!;
    }
}
