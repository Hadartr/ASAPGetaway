using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;
using ASAPGetaway.Services;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    public class PaymentsController : Controller
    {
        private readonly BookingsDAL _bookingsDal;
        private readonly TripsDAL _tripsDal;
        private readonly EmailService _emailService;
        private readonly CreditCardsDAL _creditCardsDAL;

        public PaymentsController(BookingsDAL bookingsDal, TripsDAL tripsDal,
            EmailService emailService, CreditCardsDAL creditCardsDAL)
        {
            _bookingsDal = bookingsDal;
            _tripsDal = tripsDal;
            _emailService = emailService;
            _creditCardsDAL = creditCardsDAL;
        }

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
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            decimal totalAmount = 0;
            var allBookings = _bookingsDal.GetBookingsByUserId(userId);
            foreach (var bookingId in bookingIds)
            {
                var booking = allBookings.FirstOrDefault(b => b.BookingId == bookingId);
                if (booking != null) totalAmount += booking.TotalPrice;
            }

            ViewBag.BookingIds = bookingIdsStr;
            ViewBag.TotalAmount = totalAmount;
            ViewBag.BookingsCount = bookingIds.Count;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Payment(string bookingIds, CreditCardViewModel card)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BookingIds = bookingIds;
                ViewBag.Error = "Please check your payment details.";
                return View();
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            _creditCardsDAL.SaveCreditCard(userId, card.FirstName, card.LastName,
                card.NationalId, card.CardNumber, card.ValidDate, card.CVC);

            var allBookings = _bookingsDal.GetBookingsByUserId(userId);
            var ids = bookingIds.Split(',').Select(int.Parse).ToList();

            foreach (var bookingId in ids)
            {
                _bookingsDal.UpdateBookingStatus(bookingId, "Booked");
                var booking = allBookings.FirstOrDefault(b => b.BookingId == bookingId);
                if (booking != null)
                {
                    var trip = _tripsDal.GetTripById(booking.TripId);
                    await _emailService.SendBookingConfirmationAsync(
                        User.Identity!.Name!, booking.BookingId,
                        trip?.PackageName ?? "Trip", booking.TotalPrice);
                }
            }

            return RedirectToAction("Success", new { bookingIds });
        }

        [HttpGet]
        public IActionResult Pay(int bookingId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            var bookings = _bookingsDal.GetBookingsByUserId(userId);
            var booking = bookings.FirstOrDefault(b => b.BookingId == bookingId);

            ViewBag.BookingId = bookingId;
            ViewBag.TotalAmount = booking?.TotalPrice ?? 0;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Pay(int bookingId, CreditCardViewModel card)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.BookingId = bookingId;
                ViewBag.Error = "Please check your payment details.";
                return View();
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Challenge();

            _creditCardsDAL.SaveCreditCard(userId, card.FirstName, card.LastName,
                card.NationalId, card.CardNumber, card.ValidDate, card.CVC);

            _bookingsDal.UpdateBookingStatus(bookingId, "Booked");

            var bookings = _bookingsDal.GetBookingsByUserId(userId);
            var booking = bookings.FirstOrDefault(b => b.BookingId == bookingId);
            if (booking != null)
            {
                var trip = _tripsDal.GetTripById(booking.TripId);
                await _emailService.SendBookingConfirmationAsync(
                    User.Identity!.Name!, booking.BookingId,
                    trip?.PackageName ?? "Trip", booking.TotalPrice);
            }

            return RedirectToAction("Success", new { bookingIds = bookingId.ToString() });
        }

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