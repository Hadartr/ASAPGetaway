using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    // General site reviews (not trip-specific)
    public class SiteReviewsController : Controller
    {
        private readonly SiteReviewsDAL _reviewsDal;

        public SiteReviewsController(SiteReviewsDAL reviewsDal)
        {
            _reviewsDal = reviewsDal;
        }

        // Display all site reviews with average rating
        public IActionResult Index()
        {
            var reviews = _reviewsDal.GetAllReviews();
            
            double avgRating = reviews.Count > 0 ? reviews.Average(r => r.Rating) : 0;
            ViewBag.AverageRating = avgRating;
            
            return View(reviews);
        }

        // Add new site review
        [HttpPost]
        [Authorize]
        public IActionResult Add(int rating, string comment)
        {
            if (rating < 1 || rating > 5)
            {
                TempData["Error"] = "Rating must be between 1 and 5.";
                return RedirectToAction("Index");
            }

            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["Error"] = "Comment is required.";
                return RedirectToAction("Index");
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            string? userEmail = User.Identity?.Name;

            var review = new SiteReview
            {
                UserId = userId ?? "",
                UserEmail = userEmail,
                Rating = rating,
                Comment = comment.Trim(),
                CreatedAt = DateTime.Now
            };

            _reviewsDal.AddReview(review);
            TempData["Message"] = "Thank you for your review!";

            return RedirectToAction("Index");
        }

        // Delete review (admin only)
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int reviewId)
        {
            _reviewsDal.DeleteReview(reviewId);
            TempData["Message"] = "Review deleted successfully.";
            return RedirectToAction("Index");
        }
    }
}