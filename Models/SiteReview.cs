using System;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class SiteReview
    {
        public int ReviewId { get; set; }
        
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [EmailAddress(ErrorMessage = "Invalid email address")]
        [StringLength(256)]
        public string? UserEmail { get; set; }
        
        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Rating { get; set; }
        
        [Required(ErrorMessage = "Comment is required")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 2000 characters")]
        public string Comment { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
    }
}