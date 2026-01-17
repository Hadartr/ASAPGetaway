using System;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class Review
    {
        public int ReviewId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int TripId { get; set; }
        
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Rating is required")]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        [Display(Name = "Rating")]
        public int Rating { get; set; }
        
        [Required(ErrorMessage = "Comment is required")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Comment must be between 10 and 2000 characters")]
        [Display(Name = "Comment")]
        public string Comment { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime CreatedAt { get; set; }
    }
}