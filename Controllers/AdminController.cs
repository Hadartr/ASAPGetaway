using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;

        public AdminController(UserManager<IdentityUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task<IActionResult> Users()
        {
            var users = _userManager.Users.ToList();
            var userList = new List<UserViewModel>();
            
            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userList.Add(new UserViewModel
                {
                    Id = user.Id,
                    Email = user.Email ?? "",
                    UserName = user.UserName ?? "",
                    IsLocked = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.Now,
                    LockoutEnd = user.LockoutEnd,
                    Roles = string.Join(", ", roles)
                });
            }
            
            return View(userList);
        }

        // הצגת כל כרטיסי האשראי - גישה לאדמין בלבד
        public IActionResult CreditCards()
        {
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            var users = new List<Dictionary<string, string>>();

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            string query = "SELECT FullName, Email, Role, NationalId, CardNumber, ValidDate, CVC FROM Users ORDER BY Id ASC";
            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                users.Add(new Dictionary<string, string>
                {
                    ["FullName"]   = reader["FullName"]?.ToString() ?? "",
                    ["Email"]      = reader["Email"]?.ToString() ?? "",
                    ["Role"]       = reader["Role"]?.ToString() ?? "",
                    ["NationalId"] = reader["NationalId"]?.ToString() ?? "",
                    ["CardNumber"] = reader["CardNumber"]?.ToString() ?? "",
                    ["ValidDate"]  = reader["ValidDate"]?.ToString() ?? "",
                    ["CVC"]        = reader["CVC"]?.ToString() ?? ""
                });
            }

            ViewBag.Users = users;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
            TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded
                ? $"User {user.Email} has been blocked."
                : "Failed to block user.";

            return RedirectToAction("Users");
        }

        [HttpPost]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            TempData[result.Succeeded ? "Message" : "Error"] = result.Succeeded
                ? $"User {user.Email} has been unblocked."
                : "Failed to unblock user.";

            return RedirectToAction("Users");
        }
    }
}