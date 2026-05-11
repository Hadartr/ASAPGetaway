using Microsoft.Data.SqlClient;

namespace ASAPGetaway.DAL
{
    public class CreditCardsDAL
    {
        private readonly string _connectionString;

        public CreditCardsDAL(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")!;
        }

        public void SaveCreditCard(string userId, string firstName, string lastName,
            string nationalId, string cardNumber, string validDate, string cvc)
        {
            using var conn = new SqlConnection(_connectionString);
            conn.Open();

            // מוצא את האימייל של המשתמש
            string? email = null;
            using (var cmd = new SqlCommand(
                "SELECT Email FROM AspNetUsers WHERE Id = @UserId", conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                email = cmd.ExecuteScalar()?.ToString();
            }

            if (email == null) return;

            // בודק אם המשתמש קיים בטבלת Users
            bool exists = false;
            using (var cmd = new SqlCommand(
                "SELECT COUNT(*) FROM Users WHERE Email = @Email", conn))
            {
                cmd.Parameters.AddWithValue("@Email", email);
                exists = (int)cmd.ExecuteScalar() > 0;
            }

            if (exists)
            {
                // מעדכן כרטיס קיים
                using var updateCmd = new SqlCommand(@"
                    UPDATE Users 
                    SET FirstName = @FirstName, LastName = @LastName,
                        NationalId = @NationalId, CardNumber = @CardNumber,
                        ValidDate = @ValidDate, CVC = @CVC
                    WHERE Email = @Email", conn);

                updateCmd.Parameters.AddWithValue("@FirstName", firstName);
                updateCmd.Parameters.AddWithValue("@LastName", lastName);
                updateCmd.Parameters.AddWithValue("@NationalId", nationalId);
                updateCmd.Parameters.AddWithValue("@CardNumber", cardNumber);
                updateCmd.Parameters.AddWithValue("@ValidDate", validDate);
                updateCmd.Parameters.AddWithValue("@CVC", cvc);
                updateCmd.Parameters.AddWithValue("@Email", email);
                updateCmd.ExecuteNonQuery();
            }
            else
            {
                // מוסיף משתמש חדש עם הכרטיס
                using var insertCmd = new SqlCommand(@"
                    INSERT INTO Users 
                        (FullName, Email, PasswordHash, Role, FirstName, LastName, NationalId, CardNumber, ValidDate, CVC)
                    SELECT 
                        u.UserName, u.Email, u.PasswordHash, 
                        ISNULL(r.Name, 'User'),
                        @FirstName, @LastName, @NationalId, @CardNumber, @ValidDate, @CVC
                    FROM AspNetUsers u
                    LEFT JOIN AspNetUserRoles ur ON u.Id = ur.UserId
                    LEFT JOIN AspNetRoles r ON ur.RoleId = r.Id
                    WHERE u.Email = @Email", conn);

                insertCmd.Parameters.AddWithValue("@FirstName", firstName);
                insertCmd.Parameters.AddWithValue("@LastName", lastName);
                insertCmd.Parameters.AddWithValue("@NationalId", nationalId);
                insertCmd.Parameters.AddWithValue("@CardNumber", cardNumber);
                insertCmd.Parameters.AddWithValue("@ValidDate", validDate);
                insertCmd.Parameters.AddWithValue("@CVC", cvc);
                insertCmd.Parameters.AddWithValue("@Email", email);
                insertCmd.ExecuteNonQuery();
            }
        }
    }
}