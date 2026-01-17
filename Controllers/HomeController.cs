using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.Models;
using ASAPGetaway.DAL;

namespace ASAPGetaway.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly TripsDAL _tripsDal;

        public HomeController(ILogger<HomeController> logger, TripsDAL tripsDal)
        {
            _logger = logger;
            _tripsDal = tripsDal;
        }

        // Home page - displays active trip count
        public IActionResult Index()
        {
            int tripsCount = _tripsDal.GetAllTrips().Count;
            ViewBag.TripsCount = tripsCount;
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        // Error page with request tracking
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}