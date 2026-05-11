using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;

namespace ASAPGetaway.Controllers
{
    public class VulnerableLoginController : Controller
    {
        private readonly IConfiguration _configuration;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly UserManager<IdentityUser> _userManager;

        public VulnerableLoginController(
            IConfiguration configuration,
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager)
        {
            _configuration = configuration;
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Index(string username, string password)
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            // ⚠️ פגיע במכוון ל-SQL Injection
            string query = $"SELECT * FROM Users WHERE Email='{username}' AND PasswordHash='{password}'";

            Console.WriteLine($"=== QUERY: {query} ===");

            string? loggedInEmail = null;
            string? loggedInRole = null;

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            try
            {
                using var cmd = new SqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    loggedInEmail = reader["Email"]?.ToString();
                    loggedInRole = reader["Role"]?.ToString();
                    Console.WriteLine($"=== SQL returned: {loggedInEmail} Role: {loggedInRole} ===");
                }
                else
                {
                    Console.WriteLine("=== SQL returned nothing ===");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== SQL ERROR: {ex.Message} ===");
                ViewBag.Error = ex.Message;
                ViewBag.Success = false;
                return View();
            }

            if (loggedInEmail != null)
            {
                // מוצא את המשתמש ב-Identity ומחבר אותו לאתר
                var user = await _userManager.FindByEmailAsync(loggedInEmail);
                if (user != null)
                {
                    await _signInManager.SignOutAsync();
                    await _signInManager.SignInAsync(user, isPersistent: false);
                    return RedirectToAction("Index", "Home");
                }
            }

            ViewBag.Success = false;
            return View();
        }
    }
}