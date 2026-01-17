using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;
using ASAPGetaway.Services;
using ASAPGetaway.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ASAPGetaway.Controllers
{
    // User bookings management and PDF itinerary generation
    [Authorize]
    public class MyBookingsController : Controller
    {
        private readonly BookingsDAL _bookingsDal;
        private readonly TripsDAL _tripsDal;
        private readonly WaitingListDAL _waitingDal;
        private readonly EmailService _emailService;
        private readonly UserManager<IdentityUser> _userManager;

        public MyBookingsController(
            BookingsDAL bookingsDal, 
            TripsDAL tripsDal, 
            WaitingListDAL waitingDal, 
            EmailService emailService,
            UserManager<IdentityUser> userManager)
        {
            _bookingsDal = bookingsDal;
            _tripsDal = tripsDal;
            _waitingDal = waitingDal;
            _emailService = emailService;
            _userManager = userManager;
        }

        // Display user's bookings
        public IActionResult Index()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var bookings = _bookingsDal.GetBookingsByUserId(userId);
            
            // Load trip details for each booking
            foreach (var booking in bookings)
            {
                booking.Trip = _tripsDal.GetTripById(booking.TripId);
            }
            
            // Count pending payments
            int pendingCount = bookings.Count(b => b.Status == "PendingPayment");
            ViewBag.PendingCount = pendingCount;
            
            return View(bookings);
        }

        // Cancel booking (with waiting list notification)
        [HttpPost]
        public async Task<IActionResult> Cancel(int bookingId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Verify booking belongs to user
            if (!_bookingsDal.BookingBelongsToUser(bookingId, userId))
            {
                TempData["Error"] = "Booking not found or doesn't belong to you.";
                return RedirectToAction("Index");
            }

            string? status = _bookingsDal.GetBookingStatus(bookingId);
            if (status == "Cancelled")
            {
                TempData["Error"] = "This booking is already cancelled.";
                return RedirectToAction("Index");
            }

            // Check cancellation period (7 days before trip)
            DateTime? startDate = _bookingsDal.GetTripStartDateByBookingId(bookingId);
            if (startDate == null)
            {
                TempData["Error"] = "Unable to retrieve trip start date.";
                return RedirectToAction("Index");
            }

            int daysLeft = (startDate.Value - DateTime.Now).Days;
            int requiredDays = 7;

            // Allow immediate cancellation for pending payments
            if (status != "PendingPayment" && daysLeft < requiredDays)
            {
                TempData["Error"] = $"You can cancel only {requiredDays} days or more before the trip.";
                return RedirectToAction("Index");
            }

            // Get trip ID for waiting list notification
            var bookings = _bookingsDal.GetBookingsByUserId(userId);
            var booking = bookings.FirstOrDefault(b => b.BookingId == bookingId);
            
            if (booking == null)
            {
                TempData["Error"] = "Booking not found.";
                return RedirectToAction("Index");
            }
            
            int tripId = booking.TripId;

            // Cancel booking
            _bookingsDal.CancelBooking(bookingId);
            TempData["Success"] = "Booking cancelled successfully.";

            // Notify first person in waiting list
            try
            {
                var waitingList = _waitingDal.GetWaitingListForTrip(tripId);
                
                if (waitingList.Count > 0)
                {
                    var firstInLine = waitingList[0];
                    var user = await _userManager.FindByIdAsync(firstInLine.UserId);
                    
                    if (user != null && !string.IsNullOrEmpty(user.Email))
                    {
                        var trip = _tripsDal.GetTripById(tripId);
                        
                        if (trip != null)
                        {
                            await _emailService.SendRoomAvailableAsync(
                                user.Email,
                                trip.PackageName,
                                tripId
                            );
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Log error but don't break flow
                Console.WriteLine($"Error sending waiting list notification: {ex.Message}");
            }

            return RedirectToAction("Index");
        }

        // Download booking itinerary as PDF
        public IActionResult DownloadItinerary(int bookingId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Security: verify booking belongs to user
            if (!_bookingsDal.BookingBelongsToUser(bookingId, userId))
                return Forbid();

            var bookings = _bookingsDal.GetBookingsByUserId(userId);
            var booking = bookings.FirstOrDefault(b => b.BookingId == bookingId);
            
            if (booking == null)
                return NotFound();

            var trip = _tripsDal.GetTripById(booking.TripId);
            if (trip == null)
                return NotFound();

            // Generate PDF using QuestPDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(50);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(12));

                    page.Header().Element(ComposeHeader);
                    page.Content().Element(content => ComposeContent(content, trip, booking));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.CurrentPageNumber();
                        text.Span(" / ");
                        text.TotalPages();
                    });
                });
            });

            byte[] pdfBytes = document.GeneratePdf();
            
            return File(pdfBytes, "application/pdf", $"Itinerary_{bookingId}.pdf");
        }

        // PDF Header
        void ComposeHeader(IContainer container)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Item().Text("ASAPGetaway Travel Agency").FontSize(20).Bold().FontColor(Colors.Blue.Medium);
                    column.Item().Text("Your Dream Trip Itinerary").FontSize(14).FontColor(Colors.Grey.Darken2);
                });

                row.ConstantItem(100).Height(50).Placeholder();
            });
        }

        // PDF Content
        void ComposeContent(IContainer container, Trip trip, Booking booking)
        {
            container.PaddingVertical(20).Column(column =>
            {
                column.Spacing(10);

                // Add trip image if exists
                if (!string.IsNullOrEmpty(trip.ImagePath))
                {
                    try
                    {
                        string imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", trip.ImagePath.TrimStart('/'));
                        
                        if (System.IO.File.Exists(imagePath))
                        {
                            byte[] imageData = System.IO.File.ReadAllBytes(imagePath);
                            column.Item().Image(imageData).FitWidth();
                        }
                    }
                    catch { }
                }

                column.Item().Text($"Booking Confirmation #{booking.BookingId}").FontSize(18).Bold();
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);

                // Trip details section
                column.Item().PaddingTop(10).Text("Trip Details").FontSize(16).Bold().FontColor(Colors.Blue.Medium);
                
                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Package:");
                    row.RelativeItem().Text(trip.PackageName).Bold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Destination:");
                    row.RelativeItem().Text($"{trip.Destination}, {trip.Country}").Bold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Travel Dates:");
                    row.RelativeItem().Text($"{trip.StartDate:dd MMM yyyy} - {trip.EndDate:dd MMM yyyy}").Bold();
                });

                // Booking details section
                column.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                column.Item().PaddingTop(10).Text("Booking Details").FontSize(16).Bold().FontColor(Colors.Blue.Medium);

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Number of People:");
                    row.RelativeItem().Text(booking.NumberOfPeople.ToString()).Bold();
                });

                column.Item().Row(row =>
                {
                    row.RelativeItem().Text("Total Price:");
                    row.RelativeItem().Text($"${booking.TotalPrice}").FontSize(14).Bold().FontColor(Colors.Green.Medium);
                });

                // Important information
                column.Item().PaddingTop(20).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                column.Item().PaddingTop(10).Text("Important Information").FontSize(14).Bold().FontColor(Colors.Red.Medium);
                column.Item().Text("• Please arrive 30 minutes before departure.");
                column.Item().Text("• Bring valid ID and this confirmation.");
                column.Item().Text("• Cancellations must be made 7 days before departure.");

                column.Item().PaddingTop(20).AlignCenter().Text("Have a wonderful trip! ✈️").FontSize(14).Bold().FontColor(Colors.Blue.Medium);
            });
        }
    }
}