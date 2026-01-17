using System;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class CartItem
    {
        [Required]
        [Range(1, int.MaxValue)]
        public int TripId { get; set; }
        
        [Required]
        [StringLength(200)]
        public string PackageName { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Destination { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Required]
        [Range(0.01, 100000.00, ErrorMessage = "Invalid price")]
        [DataType(DataType.Currency)]
        public decimal Price { get; set; }
        
        [Required(ErrorMessage = "Number of people is required")]
        [Range(1, 20, ErrorMessage = "Number of people must be between 1 and 20")]
        public int NumberOfPeople { get; set; }

        [StringLength(500)]
        public string? ImagePath { get; set; }
        
        public decimal TotalPrice => Price * NumberOfPeople;
    }
}