using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ASAPGetaway.Models;

namespace ASAPGetaway.DAL
{
    // Data Access Layer for waiting list (sold-out trips)
    public class WaitingListDAL
    {
        private readonly string _connStr;

        public WaitingListDAL(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Connection string not found");
        }

        // Add user to waiting list
        public void AddToWaitingList(int tripId, string userId)
        {
            string sql = "INSERT INTO WaitingList (TripId, UserId) VALUES (@TripId, @UserId)";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Check if user already waiting for this trip
        public bool IsUserAlreadyWaiting(int tripId, string userId)
        {
            string sql = @"
SELECT COUNT(*)
FROM WaitingList
WHERE TripId = @TripId AND UserId = @UserId AND IsActive = 1";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                object result = cmd.ExecuteScalar();
                return Convert.ToInt32(result) > 0;
            }
        }

        // Get waiting list for trip (FIFO order - oldest first)
        public List<WaitingListItem> GetWaitingListForTrip(int tripId)
        {
            var list = new List<WaitingListItem>();

            string sql = @"
SELECT WaitingId, TripId, UserId, JoinDate, IsActive
FROM WaitingList
WHERE TripId = @TripId AND IsActive = 1
ORDER BY JoinDate ASC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapWaitingItem(reader));
                    }
                }
            }

            return list;
        }

        // Get all waiting list entries for user
        public List<WaitingListItem> GetWaitingListForUser(string userId)
        {
            var list = new List<WaitingListItem>();

            string sql = @"
SELECT WaitingId, TripId, UserId, JoinDate, IsActive
FROM WaitingList
WHERE UserId = @UserId AND IsActive = 1
ORDER BY JoinDate DESC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapWaitingItem(reader));
                    }
                }
            }

            return list;
        }

        // Remove user from waiting list
        public void RemoveFromWaitingList(int tripId, string userId)
        {
            string sql = "DELETE FROM WaitingList WHERE TripId = @TripId AND UserId = @UserId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@UserId", userId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Map database row to WaitingListItem object
        private WaitingListItem MapWaitingItem(SqlDataReader reader)
        {
            return new WaitingListItem
            {
                WaitingId = reader.GetInt32(reader.GetOrdinal("WaitingId")),
                TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                JoinDate = reader.GetDateTime(reader.GetOrdinal("JoinDate")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };
        }
    }
}