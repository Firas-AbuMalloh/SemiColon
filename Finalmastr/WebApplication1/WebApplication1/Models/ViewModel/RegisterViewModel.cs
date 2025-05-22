using System.ComponentModel.DataAnnotations;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be between 3 and 50 characters")]
    [RegularExpression(@"^[a-zA-Z0-9_-]*$", ErrorMessage = "Username can only contain letters, numbers, underscores and hyphens")]
    [Display(Name = "Username")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address")]
    [StringLength(100, ErrorMessage = "Email cannot exceed 100 characters")]
    [Display(Name = "Email")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = null!;

    [Required(ErrorMessage = "First name is required")]
    [StringLength(50, ErrorMessage = "First name cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s-']*$", ErrorMessage = "First name can only contain letters, spaces, hyphens and apostrophes")]
    [Display(Name = "First Name")]
    public string FirstName { get; set; } = null!;

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(50, ErrorMessage = "Last name cannot exceed 50 characters")]
    [RegularExpression(@"^[a-zA-Z\s-']*$", ErrorMessage = "Last name can only contain letters, spaces, hyphens and apostrophes")]
    [Display(Name = "Last Name")]
    public string LastName { get; set; } = null!;

    [Required(ErrorMessage = "Country is required")]
    [Display(Name = "Country")]
    public string Country { get; set; } = null!;

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[0-9+\-\(\)\s]*$", ErrorMessage = "Please enter a valid phone number")]
    [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
    [Display(Name = "Phone Number")]
    public string PhoneNumber { get; set; } = null!;
}
