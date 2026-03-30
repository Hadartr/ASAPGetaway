using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;
using ASAPGetaway.Services;

namespace ASAPGetaway.Areas.Identity.Pages.Account
{
    public class ForgotPasswordModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly EmailService _emailService;

        public ForgotPasswordModel(UserManager<IdentityUser> userManager, EmailService emailService)
        {
            _userManager = userManager;
            _emailService = emailService;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            var user = await _userManager.FindByEmailAsync(Input.Email);

            if (user == null)
                return RedirectToPage("ForgotPasswordConfirmation");

            // יצירת טוקן לאיפוס סיסמה
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Page(
                "/Account/ResetPassword",
                pageHandler: null,
                values: new { token, email = user.Email },
                protocol: Request.Scheme);

            // שליחת מייל אמיתי עם הלינק
            string subject = "🔑 Reset Your Password - ASAPGetaway";
            string body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #1E40AF;'>🔑 Password Reset Request</h2>
                    <p>Hello,</p>
                    <p>We received a request to reset your password for ASAPGetaway.</p>
                    <div style='background-color: #EFF6FF; padding: 20px; border-radius: 10px; margin: 20px 0; border-left: 4px solid #3B82F6;'>
                        <p><strong>⚠️ Important:</strong> This link will expire after use.</p>
                        <p>Your old password <strong>cannot be recovered</strong> — passwords are stored encrypted using SHA-256.</p>
                    </div>
                    <p style='text-align: center; margin: 30px 0;'>
                        <a href='{resetLink}' style='background: linear-gradient(135deg, #60A5FA, #3B82F6); color: white; padding: 15px 40px; text-decoration: none; border-radius: 10px; font-weight: bold; font-size: 16px;'>
                            Reset My Password →
                        </a>
                    </p>
                    <p>If you didn't request this, you can ignore this email.</p>
                    <p style='color: #888; font-size: 0.9em;'>This is an automated email. Please do not reply.</p>
                </body>
                </html>";

            await _emailService.SendEmailAsync(user.Email, subject, body);

            return RedirectToPage("ForgotPasswordConfirmation");
        }
    }
}