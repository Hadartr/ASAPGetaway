using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ASAPGetaway.Models;

namespace ASAPGetaway.Controllers
{
    // Admin-only controller for user management
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;

        public AdminController(UserManager<IdentityUser> userManager)
        {
            _userManager = userManager;
        }

        // Display all users with their roles and lock status
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

        // Block user account (lockout for 100 years)
        [HttpPost]
        public async Task<IActionResult> BlockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.Now.AddYears(100));
            
            if (result.Succeeded)
            {
                TempData["Message"] = $"User {user.Email} has been blocked.";
            }
            else
            {
                TempData["Error"] = "Failed to block user.";
            }
            
            return RedirectToAction("Users");
        }

        // Unblock user account
        [HttpPost]
        public async Task<IActionResult> UnblockUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound();

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            
            if (result.Succeeded)
            {
                TempData["Message"] = $"User {user.Email} has been unblocked.";
            }
            else
            {
                TempData["Error"] = "Failed to unblock user.";
            }
            
            return RedirectToAction("Users");
        }
    }
}