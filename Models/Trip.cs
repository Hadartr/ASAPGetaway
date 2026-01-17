using System;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class Trip
    {
        public int TripId { get; set; }
        
        [Required(ErrorMessage = "Package name is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Package name must be between 3 and 200 characters")]
        [Display(Name = "Package Name")]
        public string PackageName { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Destination is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Destination must be between 2 and 100 characters")]
        [Display(Name = "Destination")]
        public string Destination { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country name is too long")]
        [Display(Name = "Country")]
        public string Country { get; set; } = string.Empty;
        
        [Required(ErrorMessage = "Start date is required")]
        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required(ErrorMessage = "End date is required")]
        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        [Required(ErrorMessage = "Base price is required")]
        [Display(Name = "Base Price")]
        [DataType(DataType.Currency)]
        [Range(0.01, 100000.00, ErrorMessage = "Price must be between $0.01 and $100,000")]
        public decimal BasePrice { get; set; }
        
        [Display(Name = "Discount Price")]
        [DataType(DataType.Currency)]
        [Range(0.01, 100000.00, ErrorMessage = "Discount price must be between $0.01 and $100,000")]
        public decimal? DiscountPrice { get; set; }
        
        [Display(Name = "Discount Start Date")]
        [DataType(DataType.Date)]
        public DateTime? DiscountStartDate { get; set; }
        
        [Display(Name = "Discount End Date")]
        [DataType(DataType.Date)]
        public DateTime? DiscountEndDate { get; set; }
        
        [Display(Name = "Last Booking Date")]
        [DataType(DataType.Date)]
        public DateTime? LastBookingDate { get; set; }
        
        [Display(Name = "Cancellation Days Before Departure")]
        [Range(0, 90, ErrorMessage = "Must be between 0 and 90 days")]
        public int CancellationDaysBeforeDeparture { get; set; } = 7;
        
        [Display(Name = "Enable Reminders")]
        public bool EnableReminders { get; set; } = true;
        
        [Display(Name = "Reminder Days Before Departure")]
        [Range(1, 30, ErrorMessage = "Must be between 1 and 30 days")]
        public int ReminderDaysBeforeDeparture { get; set; } = 5;
        
        [Required(ErrorMessage = "Total rooms is required")]
        [Display(Name = "Total Rooms")]
        [Range(1, 1000, ErrorMessage = "Must be between 1 and 1000 rooms")]
        public int TotalRooms { get; set; }
        
        [Display(Name = "Minimum Age")]
        [Range(0, 100, ErrorMessage = "Age must be between 0 and 100")]
        public int MinAge { get; set; }
        
        [Required(ErrorMessage = "Package type is required")]
        [StringLength(50)]
        [Display(Name = "Package Type")]
        public string PackageType { get; set; } = string.Empty;
        
        [StringLength(2000, ErrorMessage = "Description is too long")]
        [Display(Name = "Description")]
        public string? Description { get; set; }
        
        [StringLength(500)]
        [Display(Name = "Image Path")]
        public string? ImagePath { get; set; }
        
        [Display(Name = "Popularity Score")]
        [Range(0, int.MaxValue)]
        public int PopularityScore { get; set; }
        
        [Display(Name = "Is Active")]
        public bool IsActive { get; set; }
        
        public bool IsOnSale => DiscountPrice.HasValue 
            && DiscountPrice.Value < BasePrice
            && DiscountStartDate.HasValue 
            && DiscountEndDate.HasValue
            && DateTime.Now >= DiscountStartDate.Value 
            && DateTime.Now <= DiscountEndDate.Value;
        
        public decimal CurrentPrice => IsOnSale && DiscountPrice.HasValue ? DiscountPrice.Value : BasePrice;
        
        public bool CanBookNow => LastBookingDate == null || DateTime.Now.Date <= LastBookingDate.Value.Date;
        
        public bool CanCancelNow(DateTime bookingDate)
        {
            var daysUntilDeparture = (StartDate - DateTime.Now).TotalDays;
            return daysUntilDeparture >= CancellationDaysBeforeDeparture;
        }
    }
}