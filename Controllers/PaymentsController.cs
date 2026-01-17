using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;
using ASAPGetaway.Services;

namespace ASAPGetaway.Controllers
{
    // Payment processing for single or multiple bookings
    public class PaymentsController : Controller
    {
        private readonly BookingsDAL _bookingsDal;
        private readonly TripsDAL _tripsDal;
        private readonly EmailService _emailService;

        public PaymentsController(BookingsDAL bookingsDal, TripsDAL tripsDal, EmailService emailService)
        {
            _bookingsDal = bookingsDal;
            _tripsDal = tripsDal;
            _emailService = emailService;
        }

        // Payment page for cart checkout (multiple bookings)
        [HttpGet]
        public IActionResult Payment()
        {
            string? bookingIdsStr = TempData["BookingIds"] as string;
            
            if (string.IsNullOrEmpty(bookingIdsStr))
            {
                TempData["Error"] = "No bookings to pay for.";
                return RedirectToAction("Index", "Cart");
            }

            var bookingIds = bookingIdsStr.Split(',').Select(int.Parse).ToList();
            
            // Calculate total amount
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            decimal totalAmount = 0;
            var allBookings = _bookingsDal.GetBookingsByUserId(userId);

            foreach (var bookingId in bookingIds)
            {
                var booking = allBookings.FirstOrDefault(b => b.BookingId == bookingId);
                if (booking != null)
                {
                    totalAmount += booking.TotalPrice;
                }
            }

            ViewBag.BookingIds = bookingIdsStr;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.BookingsCount = bookingIds.Count;
            
            return View();
        }

        // Process payment for multiple bookings
        [HttpPost]
        public async Task<IActionResult> Payment(
            string bookingIds,
            string cardNumber,
            string expiry,
            string cvv,
            string cardHolder)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(cardNumber) ||
                string.IsNullOrWhiteSpace(expiry) ||
                string.IsNullOrWhiteSpace(cvv) ||
                string.IsNullOrWhiteSpace(cardHolder))
            {
                ViewBag.BookingIds = bookingIds;
                ViewBag.Error = "Please fill all payment fields.";
                return View();
            }

            // Update all bookings to "Booked" status and send emails
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var allBookings = _bookingsDal.GetBookingsByUserId(userId!);
            var ids = bookingIds.Split(',').Select(int.Parse).ToList();

            foreach (var bookingId in ids)
            {
                _bookingsDal.UpdateBookingStatus(bookingId, "Booked");
                
                // Send confirmation email
                var booking = allBookings.FirstOrDefault(b => b.BookingId == bookingId);
                if (booking != null)
                {
                    var trip = _tripsDal.GetTripById(booking.TripId);
                    string packageName = trip?.PackageName ?? "Trip";
                    string userEmail = User.Identity!.Name!;
                    await _emailService.SendBookingConfirmationAsync(
                        userEmail, 
                        booking.BookingId, 
                        packageName, 
                        booking.TotalPrice
                    );
                }
            }

            return RedirectToAction("Success", new { bookingIds = bookingIds });
        }

        // Payment page for single booking (Buy Now)
        [HttpGet]
        public IActionResult Pay(int bookingId)
        {
            ViewBag.BookingId = bookingId;

            // Get total amount
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var bookings = _bookingsDal.GetBookingsByUserId(userId);
            var booking = bookings.FirstOrDefault(b => b.BookingId == bookingId);

            if (booking != null)
            {
                ViewBag.TotalAmount = booking.TotalPrice;
            }
            
            return View();
        }

        // Process payment for single booking
        [HttpPost]
        public async Task<IActionResult> Pay(
            int bookingId,
            string cardNumber,
            string expiry,
            string cvv,
            string cardHolder)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(cardNumber) ||
                string.IsNullOrWhiteSpace(expiry) ||
                string.IsNullOrWhiteSpace(cvv) ||
                string.IsNullOrWhiteSpace(cardHolder))
            {
                ViewBag.BookingId = bookingId;
                ViewBag.Error = "Please fill all payment fields.";
                return View();
            }

            // Update booking status
            _bookingsDal.UpdateBookingStatus(bookingId, "Booked");

            // Send confirmation email
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                var bookings = _bookingsDal.GetBookingsByUserId(userId);
                var booking = bookings.FirstOrDefault(b => b.BookingId == bookingId);
                
                if (booking != null)
                {
                    var trip = _tripsDal.GetTripById(booking.TripId);
                    string packageName = trip?.PackageName ?? "Trip";
                    string userEmail = User.Identity!.Name!;
                    await _emailService.SendBookingConfirmationAsync(
                        userEmail,
                        booking.BookingId,
                        packageName,
                        booking.TotalPrice
                    );
                }
            }

            return RedirectToAction("Success", new { bookingIds = bookingId.ToString() });
        }

        // Payment success page
        public IActionResult Success(string bookingIds)
        {
            var ids = bookingIds.Split(',').Select(int.Parse).ToList();
            
            ViewBag.BookingIds = bookingIds;
            ViewBag.BookingsCount = ids.Count;
            ViewBag.IdsList = ids;
            
            return View();
        }
    }
}