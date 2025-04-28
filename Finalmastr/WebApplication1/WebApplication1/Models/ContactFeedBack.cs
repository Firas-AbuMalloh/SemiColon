using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SemiColon.Models;

public partial class ContactFeedBack
{
    public int Id { get; set; }

    public int? UserId { get; set; }

    [Display(Name = "Your Message")]
    [Required(ErrorMessage = "Your Message.")]
    [StringLength(maximumLength: int.MaxValue, MinimumLength = 30, ErrorMessage = "Your message must be at least 30 characters long.")]
    public string? Message { get; set; }

    public DateOnly? CreatedAt { get; set; }

    [Required(ErrorMessage = "Please enter your email address.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    [Display(Name = "E-mail")]
    [RegularExpression(@"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$", ErrorMessage = "Please enter a valid email address. Example: example@domain.com")]

    public string? Email { get; set; }

    [Required(ErrorMessage = "Please enter your phone number.")]
    [Phone(ErrorMessage = "Please enter a valid phone number.")]
    [Display(Name = "Phone")]
    [RegularExpression(@"^(\+?\d{1,3})?[\s.-]?\(?\d{3}\)?[\s.-]?\d{3}[\s.-]?\d{4}$", ErrorMessage = "Please enter a valid phone number. Example: (+962)799258206 without ().")]
    public string? Phone { get; set; }
}