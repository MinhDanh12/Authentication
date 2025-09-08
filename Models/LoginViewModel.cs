using System.ComponentModel.DataAnnotations;

namespace AuthenticationModule.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Email hoặc Username là bắt buộc")]
        [Display(Name = "Email hoặc Username")]
        public string EmailOrUsername { get; set; } = string.Empty;

        [Required(ErrorMessage = "Mật khẩu là bắt buộc")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
