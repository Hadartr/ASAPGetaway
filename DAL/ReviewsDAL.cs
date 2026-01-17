using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ASAPGetaway.Models;

namespace ASAPGetaway.DAL
{
    // Data Access Layer for trip reviews (star ratings and comments)
    public class ReviewsDAL
    {
        private readonly string _connStr;

        public ReviewsDAL(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Connection string not found");
        }

        // Add new review for a trip
        public void AddReview(Review review)
        {
            string sql = @"
INSERT INTO Reviews (TripId, UserId, Rating, Comment, CreatedAt)
VALUES (@TripId, @UserId, @Rating, @Comment, GETDATE())";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", review.TripId);
                cmd.Parameters.AddWithValue("@UserId", review.UserId);
                cmd.Parameters.AddWithValue("@Rating", review.Rating);
                cmd.Parameters.AddWithValue("@Comment", review.Comment);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Get all reviews for a trip (newest first)
        public List<Review> GetReviewsByTripId(int tripId)
        {
            var reviews = new List<Review>();

            string sql = @"
SELECT ReviewId, TripId, UserId, Rating, Comment, CreatedAt
FROM Reviews
WHERE TripId = @TripId
ORDER BY CreatedAt DESC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        reviews.Add(new Review
                        {
                            ReviewId = reader.GetInt32(0),
                            TripId = reader.GetInt32(1),
                            UserId = reader.GetString(2),
                            Rating = reader.GetInt32(3),
                            Comment = reader.GetString(4),
                            CreatedAt = reader.GetDateTime(5)
                        });
                    }
                }
            }

            return reviews;
        }
    }
}