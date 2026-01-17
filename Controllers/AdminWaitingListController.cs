using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ASAPGetaway.DAL;

namespace ASAPGetaway.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminWaitingListController : Controller
    {
        private readonly WaitingListDAL _waitingDal;
        private readonly TripsDAL _tripsDal;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminWaitingListController(WaitingListDAL waitingDal, TripsDAL tripsDal, UserManager<IdentityUser> userManager)
        {
            _waitingDal = waitingDal;
            _tripsDal = tripsDal;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var trips = _tripsDal.GetAllTrips();
            var waitingData = new List<dynamic>();

            foreach (var trip in trips)
            {
                var waitingList = _waitingDal.GetWaitingListForTrip(trip.TripId);
                if (waitingList.Count > 0)
                {
                    waitingData.Add(new
                    {
                        Trip = trip,
                        WaitingCount = waitingList.Count
                    });
                }
            }

            return View(waitingData);
        }

        public IActionResult Details(int tripId)
        {
            var trip = _tripsDal.GetTripById(tripId);
            if (trip == null)
            {
                TempData["Error"] = "Trip not found";
                return RedirectToAction("Index");
            }

            var waitingList = _waitingDal.GetWaitingListForTrip(tripId);
            
            foreach (var item in waitingList)
            {
                var user = _userManager.FindByIdAsync(item.UserId).Result;
                ViewData[$"Email_{item.UserId}"] = user?.Email ?? "Unknown";
            }

            ViewBag.Trip = trip;
            return View(waitingList);
        }

        [HttpPost]
        public IActionResult Remove(int tripId, string userId)
        {
            _waitingDal.RemoveFromWaitingList(tripId, userId);
            TempData["Success"] = "User removed from waiting list";
            return RedirectToAction("Details", new { tripId });
        }

        [HttpPost]
        public IActionResult Clear(int tripId)
        {
            var waitingList = _waitingDal.GetWaitingListForTrip(tripId);
            foreach (var item in waitingList)
            {
                _waitingDal.RemoveFromWaitingList(tripId, item.UserId);
            }
            
            TempData["Success"] = "Waiting list cleared";
            return RedirectToAction("Index");
        }
    }
}