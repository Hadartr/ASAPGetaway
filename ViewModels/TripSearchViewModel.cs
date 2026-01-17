using System.Collections.Generic;
using ASAPGetaway.Models;

namespace ASAPGetaway.ViewModels
{
    // View model for trip search results
    public class TripSearchViewModel
    {
        // User's search query
        public string SearchTerm { get; set; } = string.Empty;

        // Search results (list of matching trips)
        public List<Trip> Trips { get; set; } = new List<Trip>();
    }
}