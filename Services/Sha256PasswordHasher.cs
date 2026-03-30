using Microsoft.AspNetCore.Identity;
using System.Security.Cryptography;
using System.Text;

namespace ASAPGetaway.Services
{
    public class Sha256PasswordHasher : IPasswordHasher<IdentityUser>
    {
        public string HashPassword(IdentityUser user, string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes); // שומר כ-HEX, לא plaintext
        }

        public PasswordVerificationResult VerifyHashedPassword(
            IdentityUser user, string hashedPassword, string providedPassword)
        {
            var hash = HashPassword(user, providedPassword);
            return hash == hashedPassword
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }
}