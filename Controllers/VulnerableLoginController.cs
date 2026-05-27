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

            string query = "SELECT TOP 1 Email FROM Users " + "WHERE Email = @Email AND PasswordHash = @PasswordHash";

            //  פגיע ל-SQL Injection
            //string query = $"SELECT * FROM Users WHERE Email='{username}' AND PasswordHash='{password}'";

            string? loggedInEmail = null;

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            try
            {
                using var cmd = new SqlCommand(query, conn);
                // הפרמטרים מועברים בנפרד - SQL Server לא יפרש אותם כקוד

                cmd.Parameters.AddWithValue("@Email", username);
                cmd.Parameters.AddWithValue("@PasswordHash", password);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    loggedInEmail = reader["Email"]?.ToString();
                }
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                ViewBag.Success = false;
                return View();
            }

            if (loggedInEmail != null)
            {
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