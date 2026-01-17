using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace ASAPGetaway.DAL
{
    // Data Access Layer for wish list (saved favorite trips)
    public class WishListDAL
    {
        private readonly string _connStr;

        public WishListDAL(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Connection string not found");
        }

        // Add trip to wish list
        public void AddToWishList(int tripId, string userId)
        {
            string sql = "INSERT INTO WishList (TripId, UserId) VALUES (@TripId, @UserId)";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Check if trip already in wish list
        public bool IsTripAlreadyInWishList(int tripId, string userId)
        {
            string sql = "SELECT COUNT(*) FROM WishList WHERE TripId = @TripId AND UserId = @UserId AND IsActive = 1";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                int count = Convert.ToInt32(cmd.ExecuteScalar());
                return count > 0;
            }
        }

        // Get all trip IDs in user's wish list
        public List<int> GetWishListForUser(string userId)
        {
            var list = new List<int>();
            string sql = "SELECT TripId FROM WishList WHERE UserId = @UserId AND IsActive = 1 ORDER BY CreatedAt DESC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(reader.GetInt32(reader.GetOrdinal("TripId")));
                    }
                }
            }

            return list;
        }

        // Remove trip from wish list (hard delete)
        public void RemoveFromWishList(int tripId, string userId)
        {
            string sql = "DELETE FROM WishList WHERE TripId = @TripId AND UserId = @UserId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Deactivate wish list item (soft delete)
        public void SoftDeleteFromWishList(int tripId, string userId)
        {
            string sql = "UPDATE WishList SET IsActive = 0 WHERE TripId = @TripId AND UserId = @UserId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Clear entire wish list for user
        public void ClearWishList(string userId)
        {
            string sql = "DELETE FROM WishList WHERE UserId = @UserId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
    }
}