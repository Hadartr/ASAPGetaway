using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    // Shopping cart for multiple trips before checkout
    [Authorize]
    public class CartController : Controller
    {
        private readonly TripsDAL _tripsDal;
        private readonly BookingsDAL _bookingsDal;
        private const string CartSessionKey = "ShoppingCart";

        public CartController(TripsDAL tripsDal, BookingsDAL bookingsDal)
        {
            _tripsDal = tripsDal;
            _bookingsDal = bookingsDal;
        }

        // Display cart contents
        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        // Add trip to cart
        [HttpPost]
        public IActionResult Add(int tripId, int numberOfPeople = 1)
        {
            var trip = _tripsDal.GetTripById(tripId);
            if (trip == null)
            {
                TempData["Error"] = "Trip not found.";
                return RedirectToAction("Index", "Trips");
            }

            // Check if trip already in cart
            var cart = GetCart();
            if (cart.Any(item => item.TripId == tripId))
            {
                TempData["Error"] = "This trip is already in your cart.";
                return RedirectToAction("Index");
            }

            // Calculate price (with discount if active)
            decimal price = trip.BasePrice;
            if (trip.DiscountPrice.HasValue && 
                (!trip.DiscountEndDate.HasValue || trip.DiscountEndDate.Value >= DateTime.Now))
            {
                price = trip.DiscountPrice.Value;
            }

            // Create cart item
            var cartItem = new CartItem
            {
                TripId = trip.TripId,
                PackageName = trip.PackageName,
                Destination = trip.Destination,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Price = price,
                NumberOfPeople = numberOfPeople,
                ImagePath = trip.ImagePath
            };

            cart.Add(cartItem);
            SaveCart(cart);

            TempData["Message"] = $"{trip.PackageName} added to cart!";
            return RedirectToAction("Index");
        }

        // Remove item from cart
        [HttpPost]
        public IActionResult Remove(int tripId)
        {
            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.TripId == tripId);
            
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                TempData["Message"] = "Item removed from cart.";
            }

            return RedirectToAction("Index");
        }

        // Update number of people for cart item
        [HttpPost]
        public IActionResult UpdateQuantity(int tripId, int numberOfPeople)
        {
            if (numberOfPeople < 1)
            {
                TempData["Error"] = "Number of people must be at least 1.";
                return RedirectToAction("Index");
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(i => i.TripId == tripId);
            
            if (item != null)
            {
                item.NumberOfPeople = numberOfPeople;
                SaveCart(cart);
            }

            return RedirectToAction("Index");
        }

        // Clear entire cart
        [HttpPost]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CartSessionKey);
            TempData["Message"] = "Cart cleared.";
            return RedirectToAction("Index");
        }

        // Checkout - convert cart to bookings
        [HttpPost]
        public IActionResult Checkout()
        {
            var cart = GetCart();
            
            if (cart.Count == 0)
            {
                TempData["Error"] = "Your cart is empty.";
                return RedirectToAction("Index");
            }

            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Check 3 bookings limit
            int activeBookings = _bookingsDal.GetActiveFutureBookingsCount(userId);
            int totalAfterCheckout = activeBookings + cart.Count;

            if (totalAfterCheckout > 3)
            {
                TempData["Error"] = $"Maximum 3 active bookings allowed. You currently have {activeBookings}.";
                return RedirectToAction("Index");
            }

            // Check room availability for all trips
            foreach (var item in cart)
            {
                int bookedRooms = _bookingsDal.GetBookedCountForTrip(item.TripId);
                int totalRooms = _bookingsDal.GetTotalRoomsForTrip(item.TripId);

                if (bookedRooms >= totalRooms)
                {
                    TempData["Error"] = $"Sorry, {item.PackageName} is fully booked.";
                    return RedirectToAction("Index");
                }
            }

            // Create bookings (status: PendingPayment)
            var bookingIds = new List<int>();
            
            foreach (var item in cart)
            {
                var booking = new Booking
                {
                    TripId = item.TripId,
                    UserId = userId,
                    BookingDate = DateTime.Now,
                    NumberOfPeople = item.NumberOfPeople,
                    TotalPrice = item.Price * item.NumberOfPeople,
                    Status = "PendingPayment"
                };

                int bookingId = _bookingsDal.CreateBooking(booking);
                bookingIds.Add(bookingId);
            }

            // Clear cart after checkout
            HttpContext.Session.Remove(CartSessionKey);

            // Redirect to payment
            TempData["BookingIds"] = string.Join(",", bookingIds);
            return RedirectToAction("Payment", "Payments");
        }

        // Helper: get cart from session
        private List<CartItem> GetCart()
        {
            var cartJson = HttpContext.Session.GetString(CartSessionKey);
            
            if (string.IsNullOrEmpty(cartJson))
                return new List<CartItem>();

            return JsonSerializer.Deserialize<List<CartItem>>(cartJson) ?? new List<CartItem>();
        }

        // Helper: save cart to session
        private void SaveCart(List<CartItem> cart)
        {
            var cartJson = JsonSerializer.Serialize(cart);
            HttpContext.Session.SetString(CartSessionKey, cartJson);
        }
    }
}