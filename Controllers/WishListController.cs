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
    // Save favorite trips for later
    [Authorize]
    public class WishListController : Controller
    {
        private readonly WishListDAL _wishDal;
        private readonly TripsDAL _tripsDal;
        private const string CartSessionKey = "ShoppingCart";

        public WishListController(WishListDAL wishDal, TripsDAL tripsDal)
        {
            _wishDal = wishDal;
            _tripsDal = tripsDal;
        }

        // Display user's wish list
        public IActionResult Index()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var tripIds = _wishDal.GetWishListForUser(userId);
            
            var trips = new List<Trip>();
            foreach (var tripId in tripIds)
            {
                var trip = _tripsDal.GetTripById(tripId);
                if (trip != null)
                {
                    trips.Add(trip);
                }
            }
            
            return View(trips);
        }

        // Add trip to wish list
        public IActionResult Add(int tripId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            if (!_wishDal.IsTripAlreadyInWishList(tripId, userId))
            {
                _wishDal.AddToWishList(tripId, userId);
                TempData["Success"] = "Added to wish list!";
            }
            else
            {
                TempData["Message"] = "Already in wish list.";
            }

            return RedirectToAction("Index");
        }

        // Remove from wish list
        [HttpPost]
        public IActionResult Remove(int tripId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            _wishDal.RemoveFromWishList(tripId, userId);
            TempData["Message"] = "Removed from wish list.";
            return RedirectToAction("Index");
        }

        // Move single trip from wish list to cart
        [HttpPost]
        public IActionResult AddToCart(int tripId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var trip = _tripsDal.GetTripById(tripId);
            if (trip == null)
            {
                TempData["Error"] = "Trip not found.";
                return RedirectToAction("Index");
            }

            var cart = GetCart();
            if (cart.Any(item => item.TripId == tripId))
            {
                TempData["Message"] = "This trip is already in your cart.";
                _wishDal.RemoveFromWishList(tripId, userId);
                return RedirectToAction("Index", "Cart");
            }

            // Calculate price with discount
            decimal price = trip.BasePrice;
            if (trip.DiscountPrice.HasValue && 
                (!trip.DiscountEndDate.HasValue || trip.DiscountEndDate.Value >= DateTime.Now))
            {
                price = trip.DiscountPrice.Value;
            }

            var cartItem = new CartItem
            {
                TripId = trip.TripId,
                PackageName = trip.PackageName,
                Destination = trip.Destination,
                StartDate = trip.StartDate,
                EndDate = trip.EndDate,
                Price = price,
                NumberOfPeople = 1,
                ImagePath = trip.ImagePath
            };

            cart.Add(cartItem);
            SaveCart(cart);
            _wishDal.RemoveFromWishList(tripId, userId);

            TempData["Success"] = $"{trip.PackageName} added to cart!";
            return RedirectToAction("Index", "Cart");
        }

        // Move all wish list items to cart
        [HttpPost]
        public IActionResult AddAllToCart()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var tripIds = _wishDal.GetWishListForUser(userId);

            if (tripIds.Count == 0)
            {
                TempData["Message"] = "Your wish list is empty.";
                return RedirectToAction("Index");
            }

            var cart = GetCart();
            int addedCount = 0;

            foreach (var tripId in tripIds)
            {
                var trip = _tripsDal.GetTripById(tripId);
                if (trip == null || cart.Any(item => item.TripId == tripId))
                    continue;

                decimal price = trip.BasePrice;
                if (trip.DiscountPrice.HasValue && 
                    (!trip.DiscountEndDate.HasValue || trip.DiscountEndDate.Value >= DateTime.Now))
                {
                    price = trip.DiscountPrice.Value;
                }

                var cartItem = new CartItem
                {
                    TripId = trip.TripId,
                    PackageName = trip.PackageName,
                    Destination = trip.Destination,
                    StartDate = trip.StartDate,
                    EndDate = trip.EndDate,
                    Price = price,
                    NumberOfPeople = 1,
                    ImagePath = trip.ImagePath
                };

                cart.Add(cartItem);
                addedCount++;
            }

            SaveCart(cart);
            _wishDal.ClearWishList(userId);

            TempData["Success"] = $"Added {addedCount} trips to cart!";
            return RedirectToAction("Index", "Cart");
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