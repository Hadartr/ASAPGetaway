using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System;
using System.Security.Claims;
using ASAPGetaway.DAL;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    // Trip reviews (star ratings and comments)
    [Authorize]
    public class ReviewsController : Controller
    {
        private readonly ReviewsDAL _reviewsDal;

        public ReviewsController(ReviewsDAL reviewsDal)
        {
            _reviewsDal = reviewsDal;
        }

        // Add review for a trip
        [HttpPost]
        public IActionResult Add(int tripId, int rating, string comment)
        {
            try
            {
                string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Challenge();
                }

                if (rating < 1 || rating > 5)
                {
                    TempData["Error"] = "Invalid rating!";
                    return RedirectToAction("Details", "Trips", new { id = tripId });
                }

                if (string.IsNullOrWhiteSpace(comment))
                {
                    TempData["Error"] = "Comment is required!";
                    return RedirectToAction("Details", "Trips", new { id = tripId });
                }

                Review review = new Review
                {
                    TripId = tripId,
                    UserId = userId,
                    Rating = rating,
                    Comment = comment
                };

                _reviewsDal.AddReview(review);

                TempData["Success"] = "Thank you for your review!";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to add review: {ex.Message}";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }
        }
    }
}