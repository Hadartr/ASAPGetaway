using System;
using System.IO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using ASAPGetaway.DAL;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    // Admin controller for trip management (CRUD operations)
    [Authorize(Roles = "Admin")]
    public class AdminTripsController : Controller
    {
        private readonly TripsDAL _tripsDal;
        private readonly BookingsDAL _bookingsDal;
        private readonly IWebHostEnvironment _env;

        public AdminTripsController(TripsDAL tripsDal, BookingsDAL bookingsDal, IWebHostEnvironment env)
        {
            _tripsDal = tripsDal;
            _bookingsDal = bookingsDal;
            _env = env;
        }

        // Display all trips including inactive ones
        public IActionResult Index()
        {
            var trips = _tripsDal.GetAllTripsIncludingInactive();
            return View(trips);
        }

        // Show create trip form with default values
        [HttpGet]
        public IActionResult Create()
        {
            Trip t = new Trip
            {
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                IsActive = true,
                CancellationDaysBeforeDeparture = 7,
                EnableReminders = true,
                ReminderDaysBeforeDeparture = 5
            };

            return View(t);
        }

        // Create new trip with image upload and validations
        [HttpPost]
        public async Task<IActionResult> Create(Trip trip, IFormFile? image)
        {
            // Basic field validations
            if (string.IsNullOrWhiteSpace(trip.PackageName))
                ModelState.AddModelError("PackageName", "Package name is required");

            if (string.IsNullOrWhiteSpace(trip.Destination))
                ModelState.AddModelError("Destination", "Destination is required");

            if (string.IsNullOrWhiteSpace(trip.Country))
                ModelState.AddModelError("Country", "Country is required");

            if (trip.EndDate <= trip.StartDate)
                ModelState.AddModelError("EndDate", "End date must be after start date");

            if (trip.BasePrice <= 0)
                ModelState.AddModelError("BasePrice", "Base price must be greater than 0");

            // Discount validations
            if (trip.DiscountPrice.HasValue && trip.DiscountStartDate.HasValue && trip.DiscountEndDate.HasValue)
            {
                var discountDuration = (trip.DiscountEndDate.Value - trip.DiscountStartDate.Value).TotalDays;
                if (discountDuration > 7)
                {
                    ModelState.AddModelError("DiscountEndDate", "Discount can be active for a maximum of 7 days.");
                }
                
                if (trip.DiscountEndDate.Value >= trip.StartDate)
                {
                    ModelState.AddModelError("DiscountEndDate", "Discount end date must be before the trip start date.");
                }
                
                if (trip.DiscountPrice.Value >= trip.BasePrice)
                {
                    ModelState.AddModelError("DiscountPrice", "Discount price must be lower than base price.");
                }
            }

            // Auto-calculate last booking date (7 days before trip)
            if (!trip.LastBookingDate.HasValue)
            {
                trip.LastBookingDate = trip.StartDate.AddDays(-7);
            }

            if (!ModelState.IsValid)
                return View(trip);
                
            // Handle image upload
            if (image != null && image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("image", "Only image files are allowed.");
                    return View(trip);
                }

                string fileName = Guid.NewGuid().ToString() + extension;
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "trips");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                trip.ImagePath = "/images/trips/" + fileName;
            }

            trip.IsActive = true;

            _tripsDal.CreateTrip(trip);
            TempData["Success"] = "Trip created successfully!";
            return RedirectToAction("Index");
        }

        // Show edit trip form
        [HttpGet]
        public IActionResult Edit(int id)
        {
            var trip = _tripsDal.GetTripById(id);
            if (trip == null)
                return NotFound();

            return View(trip);
        }

        // Update existing trip
        [HttpPost]
        public async Task<IActionResult> Edit(Trip trip, IFormFile? image)
        {
            if (trip.TripId <= 0)
                return BadRequest();

            // Same validations as Create
            if (string.IsNullOrWhiteSpace(trip.PackageName))
                ModelState.AddModelError("PackageName", "Package name is required");

            if (trip.EndDate <= trip.StartDate)
                ModelState.AddModelError("EndDate", "End date must be after start date");

            if (!trip.LastBookingDate.HasValue)
            {
                trip.LastBookingDate = trip.StartDate.AddDays(-7);
            }

            if (!ModelState.IsValid)
                return View(trip);

            // Handle new image upload (delete old image if exists)
            if (image != null && image.Length > 0)
            {
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var extension = Path.GetExtension(image.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    ModelState.AddModelError("image", "Only image files are allowed.");
                    return View(trip);
                }

                // Delete old image
                if (!string.IsNullOrEmpty(trip.ImagePath))
                {
                    string oldImagePath = Path.Combine(_env.WebRootPath, trip.ImagePath.TrimStart('/'));
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                string fileName = Guid.NewGuid().ToString() + extension;
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "trips");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                string filePath = Path.Combine(uploadsFolder, fileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await image.CopyToAsync(fileStream);
                }

                trip.ImagePath = "/images/trips/" + fileName;
            }

            _tripsDal.UpdateTrip(trip);
            TempData["Success"] = "Trip updated successfully!";
            return RedirectToAction("Index");
        }

        // Deactivate trip (soft delete)
        [HttpPost]
        public IActionResult Deactivate(int id)
        {
            _tripsDal.SetTripActive(id, false);
            return RedirectToAction("Index");
        }

        // Reactivate trip
        [HttpPost]
        public IActionResult Activate(int id)
        {
            _tripsDal.SetTripActive(id, true);
            return RedirectToAction("Index");
        }

        // Delete trip permanently (only if no active bookings)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            try
            {
                var trip = _tripsDal.GetTripById(id);
                if (trip == null)
                {
                    TempData["Error"] = "Trip not found.";
                    return RedirectToAction("Index");
                }

                // Check if trip has active bookings
                int activeBookings = _bookingsDal.GetBookedCountForTrip(id);
                
                if (activeBookings > 0)
                {
                    TempData["Error"] = $"Cannot delete '{trip.PackageName}' - {activeBookings} active bookings exist.";
                    return RedirectToAction("Index");
                }

                _tripsDal.DeleteTrip(id);
                TempData["Success"] = $"Trip '{trip.PackageName}' has been permanently deleted.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting trip: {ex.Message}";
                return RedirectToAction("Index");
            }
        }
    }
}