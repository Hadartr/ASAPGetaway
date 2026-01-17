using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ASAPGetaway.Models;

namespace ASAPGetaway.DAL
{
    // Data Access Layer for bookings - handles all database operations
    public class BookingsDAL
    {
        private readonly string _connStr;

        public BookingsDAL(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Connection string not found");
        }

        // Create new booking and return its ID
        public int CreateBooking(Booking booking)
        {
            string sql = @"
INSERT INTO Bookings
(TripId, UserId, BookingDate, NumberOfPeople, TotalPrice, Status)
VALUES
(@TripId, @UserId, @BookingDate, @NumberOfPeople, @TotalPrice, @Status);
SELECT SCOPE_IDENTITY();";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", booking.TripId);
                cmd.Parameters.AddWithValue("@UserId", booking.UserId);
                cmd.Parameters.AddWithValue("@BookingDate", booking.BookingDate);
                cmd.Parameters.AddWithValue("@NumberOfPeople", booking.NumberOfPeople);
                cmd.Parameters.AddWithValue("@TotalPrice", booking.TotalPrice);
                cmd.Parameters.AddWithValue("@Status", booking.Status);

                conn.Open();
                object result = cmd.ExecuteScalar();
                int newBookingId = Convert.ToInt32(result);

                // Update trip popularity score
                string updatePopularity = "UPDATE Trips SET PopularityScore = PopularityScore + 1 WHERE TripId = @TripId";
                using (SqlCommand cmdPop = new SqlCommand(updatePopularity, conn))
                {
                    cmdPop.Parameters.AddWithValue("@TripId", booking.TripId);
                    cmdPop.ExecuteNonQuery();
                }

                return newBookingId;
            }
        }

        // Update booking status (PendingPayment -> Booked)
        public void UpdateBookingStatus(int bookingId, string status)
        {
            string sql = "UPDATE Bookings SET Status = @Status WHERE BookingId = @BookingId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@Status", status);
                cmd.Parameters.AddWithValue("@BookingId", bookingId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Get all bookings for a user
        public List<Booking> GetBookingsByUserId(string userId)
        {
            var list = new List<Booking>();

            string sql = @"
SELECT BookingId, TripId, UserId, BookingDate, NumberOfPeople, TotalPrice, Status
FROM Bookings
WHERE UserId = @UserId
ORDER BY BookingDate DESC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapBooking(reader));
                    }
                }
            }

            return list;
        }

        // Verify booking belongs to user (security check)
        public bool BookingBelongsToUser(int bookingId, string userId)
        {
            string sql = "SELECT COUNT(*) FROM Bookings WHERE BookingId = @BookingId AND UserId = @UserId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BookingId", bookingId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        // Get trip start date for cancellation validation
        public DateTime? GetTripStartDateByBookingId(int bookingId)
        {
            string sql = @"
SELECT t.StartDate
FROM Bookings b
INNER JOIN Trips t ON b.TripId = t.TripId
WHERE b.BookingId = @BookingId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BookingId", bookingId);
                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return null;

                return Convert.ToDateTime(result);
            }
        }

        // Get booking status
        public string? GetBookingStatus(int bookingId)
        {
            string sql = "SELECT Status FROM Bookings WHERE BookingId = @BookingId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BookingId", bookingId);
                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return null;

                return result.ToString();
            }
        }

        // Cancel booking
        public void CancelBooking(int bookingId)
        {
            string sql = "UPDATE Bookings SET Status = 'Cancelled' WHERE BookingId = @BookingId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@BookingId", bookingId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Count active future bookings for user (limit: 3)
        public int GetActiveFutureBookingsCount(string userId)
        {
            string sql = @"
SELECT COUNT(*)
FROM Bookings b
INNER JOIN Trips t ON b.TripId = t.TripId
WHERE b.UserId = @UserId
  AND (b.Status = 'Booked' OR b.Status = 'PendingPayment')
  AND t.EndDate >= CAST(GETDATE() AS date)";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        // Count active bookings for trip (room availability)
        public int GetBookedCountForTrip(int tripId)
        {
            string sql = @"
SELECT COUNT(*)
FROM Bookings
WHERE TripId = @TripId
  AND (Status = 'Booked' OR Status = 'PendingPayment')";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        // Get total rooms for trip
        public int GetTotalRoomsForTrip(int tripId)
        {
            string sql = "SELECT TotalRooms FROM Trips WHERE TripId = @TripId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result);
            }
        }

        // Get bookings needing reminders today (for background service)
        public List<(int BookingId, int TripId, string UserId, string Email)> GetBookingsNeedingRemindersToday()
        {
            var list = new List<(int, int, string, string)>();

            string sql = @"
SELECT DISTINCT b.BookingId, b.TripId, b.UserId, u.Email
FROM Bookings b
INNER JOIN Trips t ON b.TripId = t.TripId
INNER JOIN AspNetUsers u ON b.UserId = u.Id
WHERE b.Status = 'Booked'
  AND t.EnableReminders = 1
  AND DATEDIFF(DAY, CAST(GETDATE() AS date), t.StartDate) = t.ReminderDaysBeforeDeparture";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        int bookingId = reader.GetInt32(0);
                        int tripId = reader.GetInt32(1);
                        string userId = reader.GetString(2);
                        string email = reader.GetString(3);
                        
                        list.Add((bookingId, tripId, userId, email));
                    }
                }
            }

            return list;
        }

        // Map database row to Booking object
        private Booking MapBooking(SqlDataReader reader)
        {
            return new Booking
            {
                BookingId = reader.GetInt32(reader.GetOrdinal("BookingId")),
                TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                BookingDate = reader.GetDateTime(reader.GetOrdinal("BookingDate")),
                NumberOfPeople = reader.GetInt32(reader.GetOrdinal("NumberOfPeople")),
                TotalPrice = reader.GetDecimal(reader.GetOrdinal("TotalPrice")),
                Status = reader.GetString(reader.GetOrdinal("Status"))
            };
        }
    }
}