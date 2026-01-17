using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;

namespace ASAPGetaway.Controllers
{
    // Trip browsing and filtering
    public class TripsController : Controller
    {
        private readonly TripsDAL _tripsDal;
        private readonly BookingsDAL _bookingsDal;
        private readonly ReviewsDAL _reviewsDal;

        public TripsController(TripsDAL tripsDal, BookingsDAL bookingsDal, ReviewsDAL reviewsDal)
        {
            _tripsDal = tripsDal;
            _bookingsDal = bookingsDal;
            _reviewsDal = reviewsDal;
        }

        // Browse trips with filters
        public IActionResult Index(
            string? country,
            string? packageType,
            decimal? minPrice,
            decimal? maxPrice,
            DateTime? travelFrom,
            DateTime? travelTo,
            bool onSaleOnly = false,
            string? sort = null)
        {
            var trips = _tripsDal.GetTrips(
                country: country,
                packageType: packageType,
                sort: sort,
                onSaleOnly: onSaleOnly,
                minPrice: minPrice,
                maxPrice: maxPrice,
                travelFrom: travelFrom,
                travelTo: travelTo
            );

            // Calculate available rooms for each trip
            var availableRooms = new Dictionary<int, int>();
            foreach (var trip in trips)
            {
                int bookedRooms = _bookingsDal.GetBookedCountForTrip(trip.TripId);
                availableRooms[trip.TripId] = trip.TotalRooms - bookedRooms;
            }
            
            ViewBag.AvailableRooms = availableRooms;

            // Pass filter values back to view
            ViewBag.SelectedCountry = country;
            ViewBag.SelectedPackageType = packageType;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.TravelFrom = travelFrom?.ToString("yyyy-MM-dd");
            ViewBag.TravelTo = travelTo?.ToString("yyyy-MM-dd");
            ViewBag.OnSaleOnly = onSaleOnly;
            ViewBag.Sort = sort;

            return View(trips);
        }

        // Trip details with reviews
        public IActionResult Details(int id)
        {
            var trip = _tripsDal.GetTripById(id);
            if (trip == null)
                return NotFound();

            int bookedRooms = _bookingsDal.GetBookedCountForTrip(id);
            int availableRooms = trip.TotalRooms - bookedRooms;
            
            ViewBag.BookedRooms = bookedRooms;
            ViewBag.AvailableRooms = availableRooms;
            ViewBag.IsFull = availableRooms <= 0;

            var reviews = _reviewsDal.GetReviewsByTripId(id);
            ViewBag.Reviews = reviews;

            return View(trip);
        }
    }
}