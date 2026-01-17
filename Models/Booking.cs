using System;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class Booking
    {
        public int BookingId { get; set; }

        [Required(ErrorMessage = "Trip ID is required")]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid trip ID")]
        public int TripId { get; set; }

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime BookingDate { get; set; }

        [Required(ErrorMessage = "Number of people is required")]
        [Range(1, 20, ErrorMessage = "Number of people must be between 1 and 20")]
        [Display(Name = "Number of People")]
        public int NumberOfPeople { get; set; }

        [Required]
        [Range(0.01, 1000000.00, ErrorMessage = "Invalid price")]
        [DataType(DataType.Currency)]
        [Display(Name = "Total Price")]
        public decimal TotalPrice { get; set; }

        [Required]
        [StringLength(50)]
        [RegularExpression("^(PendingPayment|Booked|Cancelled)$", 
            ErrorMessage = "Status must be PendingPayment, Booked, or Cancelled")]
        public string Status { get; set; } = string.Empty;
        
        public Trip? Trip { get; set; }
    }
}