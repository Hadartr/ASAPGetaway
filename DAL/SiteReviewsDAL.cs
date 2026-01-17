using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ASAPGetaway.Models;

namespace ASAPGetaway.DAL
{
    // Data Access Layer for general site reviews (not trip-specific)
    public class SiteReviewsDAL
    {
        private readonly string _connStr;

        public SiteReviewsDAL(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Connection string not found");
        }

        // Add new site review
        public void AddReview(SiteReview review)
        {
            string sql = @"
INSERT INTO SiteReviews (UserId, UserEmail, Rating, Comment, CreatedAt)
VALUES (@UserId, @UserEmail, @Rating, @Comment, @CreatedAt)";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@UserId", review.UserId);
                cmd.Parameters.AddWithValue("@UserEmail", review.UserEmail ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@Rating", review.Rating);
                cmd.Parameters.AddWithValue("@Comment", review.Comment);
                cmd.Parameters.AddWithValue("@CreatedAt", review.CreatedAt);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Get all site reviews (newest first)
        public List<SiteReview> GetAllReviews()
        {
            var list = new List<SiteReview>();

            string sql = @"
SELECT ReviewId, UserId, UserEmail, Rating, Comment, CreatedAt
FROM SiteReviews
ORDER BY CreatedAt DESC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(MapReview(reader));
                    }
                }
            }

            return list;
        }

        // Delete review (admin only)
        public void DeleteReview(int reviewId)
        {
            string sql = "DELETE FROM SiteReviews WHERE ReviewId = @ReviewId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@ReviewId", reviewId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Map database row to SiteReview object
        private SiteReview MapReview(SqlDataReader reader)
        {
            return new SiteReview
            {
                ReviewId = reader.GetInt32(reader.GetOrdinal("ReviewId")),
                UserId = reader.GetString(reader.GetOrdinal("UserId")),
                UserEmail = reader.IsDBNull(reader.GetOrdinal("UserEmail")) 
                    ? null 
                    : reader.GetString(reader.GetOrdinal("UserEmail")),
                Rating = reader.GetInt32(reader.GetOrdinal("Rating")),
                Comment = reader.GetString(reader.GetOrdinal("Comment")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}