using System;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class UserViewModel
    {
        public string Id { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(256)]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string UserName { get; set; } = string.Empty;

        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string? ConfirmPassword { get; set; }

        [Display(Name = "Is Locked")]
        public bool IsLocked { get; set; }

        public DateTimeOffset? LockoutEnd { get; set; }

        [Display(Name = "Roles")]
        public string Roles { get; set; } = string.Empty;
    }
}