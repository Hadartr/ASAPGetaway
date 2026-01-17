using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.DAL;
using ASAPGetaway.Services;

namespace ASAPGetaway.Controllers
{
    // Waiting list for sold-out trips
    [Authorize]
    public class WaitingListController : Controller
    {
        private readonly WaitingListDAL _waitingDal;
        private readonly TripsDAL _tripsDal;
        private readonly EmailService _emailService;

        public WaitingListController(WaitingListDAL waitingDal, TripsDAL tripsDal, EmailService emailService)
        {
            _waitingDal = waitingDal;
            _tripsDal = tripsDal;
            _emailService = emailService;
        }

        // Display user's waiting list with positions
        public IActionResult My()
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            var items = _waitingDal.GetWaitingListForUser(userId);
            
            // Load trip details
            foreach (var item in items)
            {
                item.Trip = _tripsDal.GetTripById(item.TripId);
            }
            
            // Calculate position in queue for each trip (FIFO)
            var positions = new Dictionary<int, int>();
            
            foreach (var item in items)
            {
                var fullWaitingList = _waitingDal.GetWaitingListForTrip(item.TripId);
                
                // Find user's position
                int position = 1;
                for (int i = 0; i < fullWaitingList.Count; i++)
                {
                    if (fullWaitingList[i].UserId == userId && fullWaitingList[i].TripId == item.TripId)
                    {
                        position = i + 1;
                        break;
                    }
                }
                
                positions[item.TripId] = position;
            }
            
            ViewBag.Positions = positions;
            
            return View(items);
        }

        // Add to waiting list with confirmation email
        [HttpGet]
        public async Task<IActionResult> Add(int tripId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            // Check if already waiting
            if (_waitingDal.IsUserAlreadyWaiting(tripId, userId))
            {
                TempData["Message"] = "You are already on the waiting list for this trip.";
                return RedirectToAction("Details", "Trips", new { id = tripId });
            }

            _waitingDal.AddToWaitingList(tripId, userId);

            // Send confirmation email
            var trip = _tripsDal.GetTripById(tripId);
            if (trip != null)
            {
                string userEmail = User.Identity!.Name!;
                var waitingList = _waitingDal.GetWaitingListForTrip(tripId);
                int position = waitingList.Count;
                
                await _emailService.SendWaitingListConfirmationAsync(userEmail, trip.PackageName, position);
            }

            TempData["Message"] = "You have been added to the waiting list!";
            return RedirectToAction("My");
        }

        // Remove from waiting list
        [HttpPost]
        public IActionResult Remove(int tripId)
        {
            string? userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
                return Challenge();

            _waitingDal.RemoveFromWaitingList(tripId, userId);
            TempData["Message"] = "You have been removed from the waiting list.";
            return RedirectToAction("My");
        }
    }
}