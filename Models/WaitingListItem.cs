using System;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Models
{
    public class WaitingListItem
    {
        public int WaitingId { get; set; }
        
        [Required]
        [Range(1, int.MaxValue)]
        public int TripId { get; set; }
        
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = string.Empty;
        
        [Required]
        [DataType(DataType.DateTime)]
        public DateTime JoinDate { get; set; }
        
        public bool IsActive { get; set; }
        
        public Trip? Trip { get; set; }
    }
}