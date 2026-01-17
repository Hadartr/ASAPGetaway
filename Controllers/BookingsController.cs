using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    [Authorize]
    public class BookingsController : Controller
    {
        private readonly BookingsDAL _bookingsDal;
        private readonly TripsDAL _tripsDal;
        private readonly WaitingListDAL _waitingDal;

        public BookingsController(
            BookingsDAL bookingsDal,
            TripsDAL tripsDal,
            WaitingListDAL waitingDal)
        {
            _bookingsDal = bookingsDal;
            _tripsDal = tripsDal;
            _waitingDal = waitingDal;
        }

        // Show booking form for regular booking
        public IActionResult Create(int tripId)
        {
            var trip = _tripsDal.GetTripById(tripId);
            if (trip == null)
                return NotFound();

            // Check if booking period has ended
            if (trip.LastBookingDate.HasValue && DateTime.Now.Date > trip.LastBookingDate.Value.Date)
            {
                TempData["Error"] = "Booking period for this trip has ended.";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }

            ViewBag.PackageName = trip.PackageName;
            ViewBag.UnitPrice = GetUnitPrice(trip);

            return View(new Booking
            {
                TripId = tripId,
                NumberOfPeople = 1
            });
        }

        // Quick booking for 1 person - skips form and goes straight to payment
        public IActionResult BuyNow(int tripId)
        {
            var trip = _tripsDal.GetTripById(tripId);
            if (trip == null)
                return NotFound();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Check booking period
            if (trip.LastBookingDate.HasValue && DateTime.Now.Date > trip.LastBookingDate.Value.Date)
            {
                TempData["Error"] = "Booking period for this trip has ended.";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }

            // Limit: maximum 3 active future bookings per user
            if (_bookingsDal.GetActiveFutureBookingsCount(userId) >= 3)
            {
                TempData["Error"] = "You already have 3 active future bookings.";
                return RedirectToAction("Create", new { tripId });
            }

            // Check room availability
            int totalRooms = _bookingsDal.GetTotalRoomsForTrip(trip.TripId);
            int bookedCount = _bookingsDal.GetBookedCountForTrip(trip.TripId);

            if (bookedCount >= totalRooms)
            {
                // Add to waiting list if trip is full
                if (_waitingDal.IsUserAlreadyWaiting(trip.TripId, userId))
                {
                    TempData["Error"] = "This trip is full. You are already on the waiting list.";
                    return RedirectToAction("Create", new { tripId });
                }

                _waitingDal.AddToWaitingList(trip.TripId, userId);
                TempData["Error"] = "This trip is full. You have been added to the waiting list.";
                return RedirectToAction("Create", new { tripId });
            }

            // Create immediate booking for 1 person
            Booking booking = new Booking
            {
                TripId = tripId,
                NumberOfPeople = 1,
                UserId = userId,
                BookingDate = DateTime.Now,
                Status = "PendingPayment",
                TotalPrice = GetUnitPrice(trip) * 1
            };

            int bookingId = _bookingsDal.CreateBooking(booking);

            return RedirectToAction("Pay", "Payments", new { bookingId });
        }

        // Process booking form submission
        [HttpPost]
        public IActionResult Create(Booking booking)
        {
            var trip = _tripsDal.GetTripById(booking.TripId);
            if (trip == null)
                return NotFound();

            string userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            // Check booking period
            if (trip.LastBookingDate.HasValue && DateTime.Now.Date > trip.LastBookingDate.Value.Date)
            {
                SetViewData(trip, "Booking period for this trip has ended.");
                return View(booking);
            }

            // Limit: maximum 3 active future bookings
            if (_bookingsDal.GetActiveFutureBookingsCount(userId) >= 3)
            {
                SetViewData(trip, "You already have 3 active future bookings.");
                return View(booking);
            }

            // Check room availability
            int totalRooms = _bookingsDal.GetTotalRoomsForTrip(trip.TripId);
            int bookedCount = _bookingsDal.GetBookedCountForTrip(trip.TripId);

            if (bookedCount >= totalRooms)
            {
                // Add to waiting list if trip is full
                if (_waitingDal.IsUserAlreadyWaiting(trip.TripId, userId))
                {
                    SetViewData(trip, "This trip is full. You are already on the waiting list.");
                    return View(booking);
                }

                _waitingDal.AddToWaitingList(trip.TripId, userId);
                SetViewData(trip, "This trip is full. You have been added to the waiting list.");
                return View(booking);
            }

            // Create booking
            booking.UserId = userId;
            booking.BookingDate = DateTime.Now;
            booking.Status = "PendingPayment";
            booking.TotalPrice = GetUnitPrice(trip) * booking.NumberOfPeople;

            int bookingId = _bookingsDal.CreateBooking(booking);

            return RedirectToAction("Pay", "Payments", new { bookingId });
        }

        // Helper: set view data for error display
        private void SetViewData(Trip trip, string error)
        {
            ViewBag.PackageName = trip.PackageName;
            ViewBag.UnitPrice = GetUnitPrice(trip);
            ViewBag.Error = error;
        }

        // Helper: calculate unit price (with discount if active)
        private decimal GetUnitPrice(Trip trip)
        {
            if (trip.IsOnSale)
            {
                return trip.DiscountPrice!.Value;
            }

            return trip.BasePrice;
        }
    }
}