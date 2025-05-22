using System.ComponentModel.DataAnnotations;

namespace SemiColon.Models.ViewModel
{
    public class updateProfile
    {
        [Required(ErrorMessage = "Username is required")]
        [StringLength(50, ErrorMessage = "Username cannot be longer than 50 characters")]
        public string Username { get; set; } = null!;

        [DataType(DataType.Password)]
        public string OldPassword { get; set; } = null!;

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "New Password must be at least 6 characters")]
        public string NewPassword { get; set; } = null!;

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = null!;

        [StringLength(50)]
        public string? FirstName { get; set; }

        [StringLength(50)]
        public string? LastName { get; set; }

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? PhoneNumber { get; set; }

        public bool ChangePassword { get; set; }
    }
}
