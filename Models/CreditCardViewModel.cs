using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class CreditCardViewModel
    {
        [Required]
        [RegularExpression(@"^[A-Za-z\u0590-\u05FF]{2,50}$", 
            ErrorMessage = "First name must contain letters only")]
        public string FirstName { get; set; }

        [Required]
        [RegularExpression(@"^[A-Za-z\u0590-\u05FF][A-Za-z\u0590-\u05FF\- ]{1,49}$", 
            ErrorMessage = "Last name must contain letters, spaces or hyphens only")]
        public string LastName { get; set; }

        [Required]
        [RegularExpression(@"^\d{9}$", 
            ErrorMessage = "ID must be exactly 9 digits")]
        public string NationalId { get; set; }

        [Required]
        [RegularExpression(@"^\d{4} \d{4} \d{4} \d{4}$", 
            ErrorMessage = "Card number format: 1234 5567 8901 2345")]
        public string CardNumber { get; set; }

        [Required]
        [RegularExpression(@"^(0[1-9]|1[0-2])\/\d{2}$", 
            ErrorMessage = "Valid date format: MM/YY")]
        public string ValidDate { get; set; }

        [Required]
        [RegularExpression(@"^\d{3}$", 
            ErrorMessage = "CVC must be exactly 3 digits")]
        public string CVC { get; set; }
    }
}