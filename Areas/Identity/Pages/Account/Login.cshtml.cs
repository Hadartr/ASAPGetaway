using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace ASAPGetaway.Areas.Identity.Pages.Account
{
    /// Login page model - handles user authentication

    public class LoginModel : PageModel
    {
        // Dependency injection for authentication services
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(SignInManager<IdentityUser> signInManager, ILogger<LoginModel> logger)
        {
            _signInManager = signInManager;
            _logger = logger;
        }

        // Model binding - binds form data to this property on POST
        [BindProperty]
        public InputModel Input { get; set; }

        // URL to redirect to after successful login
        public string ReturnUrl { get; set; }

        // Temporary error message (survives redirects)
        [TempData]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Input model for login form - represents the data user submits
        /// Uses data annotations for validation
        /// </summary>
        public class InputModel
        {
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            public string Email { get; set; }

            [Required(ErrorMessage = "Password is required")]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        /// <summary>
        /// GET request handler - displays the login page
        /// </summary>
        public async Task OnGetAsync(string returnUrl = null)
        {
            // Display error message if exists
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            // Set default return URL to home page if not specified
            returnUrl ??= Url.Content("~/");

            // Clear any existing external authentication cookies
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ReturnUrl = returnUrl;
        }

        /// <summary>
        /// POST request handler - processes login form submission
        /// </summary>
        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            // Check if form data is valid (email format, required fields, etc.)
            if (ModelState.IsValid)
            {
                // Attempt to sign in user with provided credentials
                // lockoutOnFailure: false = don't lock account after failed attempts
                var result = await _signInManager.PasswordSignInAsync(
                    Input.Email, 
                    Input.Password, 
                    Input.RememberMe, 
                    lockoutOnFailure: false
                );
                
                if (result.Succeeded)
                {
                    // Login successful - log the event and redirect
                    _logger.LogInformation("User logged in successfully.");
                    return LocalRedirect(returnUrl);
                }
                
                if (result.RequiresTwoFactor)
                {
                    // User has 2FA enabled - redirect to 2FA page
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                
                if (result.IsLockedOut)
                {
                    // Account is locked due to too many failed attempts
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    // Login failed - invalid email or password
                    ModelState.AddModelError(string.Empty, "Invalid email or password.");
                    return Page();
                }
            }

            // Model validation failed - redisplay form with errors
            return Page();
        }
    }
}