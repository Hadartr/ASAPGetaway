using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using ASAPGetaway.Models;

namespace ASAPGetaway.DAL
{
    // Data Access Layer for trips - handles trip database operations
    public class TripsDAL
    {
        private readonly string _connStr;

        public TripsDAL(IConfiguration config)
        {
            _connStr = config.GetConnectionString("DefaultConnection")
                      ?? throw new Exception("Connection string not found");
        }

        // Get all active trips
        public List<Trip> GetAllTrips() => GetTrips(null, null, null, false, null, null, null, null);

        // Get trips with filters
        public List<Trip> GetTrips(
            string? country,
            string? packageType,
            string? sort,
            bool onSaleOnly,
            decimal? minPrice,
            decimal? maxPrice,
            DateTime? travelFrom,
            DateTime? travelTo)
        {
            var trips = new List<Trip>();
            string sql = @"
SELECT TripId, PackageName, Destination, Country, StartDate, EndDate,
       BasePrice, DiscountPrice, DiscountEndDate, TotalRooms, MinAge,
       PackageType, Description, ImagePath, PopularityScore, IsActive
FROM Trips
WHERE IsActive = 1";

            // Dynamic filter building
            if (!string.IsNullOrWhiteSpace(country))
                sql += " AND Country = @Country";

            if (!string.IsNullOrWhiteSpace(packageType))
                sql += " AND PackageType = @PackageType";

            if (onSaleOnly)
                sql += " AND DiscountPrice IS NOT NULL AND DiscountPrice < BasePrice";

            string effectivePrice = @"CASE 
                WHEN DiscountPrice IS NOT NULL AND (DiscountEndDate IS NULL OR DiscountEndDate >= GETDATE())
                THEN DiscountPrice ELSE BasePrice END";

            if (minPrice.HasValue)
                sql += $" AND ({effectivePrice}) >= @MinPrice";

            if (maxPrice.HasValue)
                sql += $" AND ({effectivePrice}) <= @MaxPrice";

            if (travelFrom.HasValue)
                sql += " AND StartDate >= @TravelFrom";

            if (travelTo.HasValue)
                sql += " AND EndDate <= @TravelTo";

            sql += GetOrderBy(sort);

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                if (!string.IsNullOrWhiteSpace(country))
                    cmd.Parameters.AddWithValue("@Country", country);

                if (!string.IsNullOrWhiteSpace(packageType))
                    cmd.Parameters.AddWithValue("@PackageType", packageType);

                if (minPrice.HasValue)
                    cmd.Parameters.AddWithValue("@MinPrice", minPrice.Value);

                if (maxPrice.HasValue)
                    cmd.Parameters.AddWithValue("@MaxPrice", maxPrice.Value);

                if (travelFrom.HasValue)
                    cmd.Parameters.AddWithValue("@TravelFrom", travelFrom.Value);

                if (travelTo.HasValue)
                    cmd.Parameters.AddWithValue("@TravelTo", travelTo.Value);

                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        trips.Add(MapTrip(reader));
                }
            }

            return trips;
        }

        // Search trips by keyword
        public List<Trip> SearchTrips(string searchTerm)
        {
            var trips = new List<Trip>();
            string sql = @"
SELECT TripId, PackageName, Destination, Country, StartDate, EndDate,
       BasePrice, DiscountPrice, DiscountEndDate, TotalRooms, MinAge,
       PackageType, Description, ImagePath, PopularityScore, IsActive
FROM Trips
WHERE IsActive = 1
  AND (PackageName LIKE @term OR Destination LIKE @term OR Country LIKE @term)
ORDER BY StartDate ASC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@term", "%" + searchTerm + "%");
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        trips.Add(MapTrip(reader));
                }
            }

            return trips;
        }

        // Get single trip by ID
        public Trip? GetTripById(int tripId)
        {
            Trip? trip = null;
            string sql = @"
SELECT TripId, PackageName, Destination, Country, StartDate, EndDate,
       BasePrice, DiscountPrice, DiscountEndDate, TotalRooms, MinAge,
       PackageType, Description, ImagePath, PopularityScore, IsActive
FROM Trips
WHERE TripId = @TripId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                        trip = MapTrip(reader);
                }
            }

            return trip;
        }

        // Get sort clause based on sort parameter
        private string GetOrderBy(string? sort) => sort switch
        {
            "price_asc" => " ORDER BY BasePrice ASC",
            "price_desc" => " ORDER BY BasePrice DESC",
            "date_asc" => " ORDER BY StartDate ASC",
            "date_desc" => " ORDER BY StartDate DESC",
            "country_asc" => " ORDER BY Country ASC",
            "popular" => " ORDER BY PopularityScore DESC, StartDate ASC",
            "category" => " ORDER BY PackageType ASC, StartDate ASC",
            _ => " ORDER BY StartDate ASC"
        };

        // Admin: get all trips including inactive
        public List<Trip> GetAllTripsIncludingInactive()
        {
            var trips = new List<Trip>();
            string sql = @"
SELECT TripId, PackageName, Destination, Country, StartDate, EndDate,
       BasePrice, DiscountPrice, DiscountEndDate, TotalRooms, MinAge,
       PackageType, Description, ImagePath, PopularityScore, IsActive
FROM Trips
ORDER BY TripId DESC";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                conn.Open();
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                        trips.Add(MapTrip(reader));
                }
            }

            return trips;
        }

        // Admin: create new trip
        public int CreateTrip(Trip trip)
        {
            string sql = @"
INSERT INTO Trips
(PackageName, Destination, Country, StartDate, EndDate, BasePrice, DiscountPrice, 
 DiscountEndDate, TotalRooms, MinAge, PackageType, Description, ImagePath, PopularityScore, IsActive)
VALUES
(@PackageName, @Destination, @Country, @StartDate, @EndDate, @BasePrice, @DiscountPrice,
 @DiscountEndDate, @TotalRooms, @MinAge, @PackageType, @Description, @ImagePath, @PopularityScore, @IsActive);
SELECT SCOPE_IDENTITY();";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                AddTripParameters(cmd, trip);
                conn.Open();
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        // Admin: update trip
        public void UpdateTrip(Trip trip)
        {
            string sql = @"
UPDATE Trips
SET PackageName = @PackageName, Destination = @Destination, Country = @Country,
    StartDate = @StartDate, EndDate = @EndDate, BasePrice = @BasePrice,
    DiscountPrice = @DiscountPrice, DiscountEndDate = @DiscountEndDate,
    TotalRooms = @TotalRooms, MinAge = @MinAge, PackageType = @PackageType,
    Description = @Description, ImagePath = @ImagePath,
    PopularityScore = @PopularityScore, IsActive = @IsActive
WHERE TripId = @TripId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", trip.TripId);
                AddTripParameters(cmd, trip);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Admin: activate/deactivate trip
        public void SetTripActive(int tripId, bool isActive)
        {
            string sql = "UPDATE Trips SET IsActive = @IsActive WHERE TripId = @TripId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                cmd.Parameters.AddWithValue("@IsActive", isActive);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Admin: delete trip permanently
        public void DeleteTrip(int tripId)
        {
            string sql = "DELETE FROM Trips WHERE TripId = @TripId";

            using (SqlConnection conn = new SqlConnection(_connStr))
            using (SqlCommand cmd = new SqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@TripId", tripId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }

        // Helper: add trip parameters to command
        private void AddTripParameters(SqlCommand cmd, Trip trip)
        {
            cmd.Parameters.AddWithValue("@PackageName", trip.PackageName);
            cmd.Parameters.AddWithValue("@Destination", trip.Destination);
            cmd.Parameters.AddWithValue("@Country", trip.Country);
            cmd.Parameters.AddWithValue("@StartDate", trip.StartDate);
            cmd.Parameters.AddWithValue("@EndDate", trip.EndDate);
            cmd.Parameters.AddWithValue("@BasePrice", trip.BasePrice);
            cmd.Parameters.AddWithValue("@DiscountPrice",
                trip.DiscountPrice.HasValue ? (object)trip.DiscountPrice.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@DiscountEndDate",
                trip.DiscountEndDate.HasValue ? (object)trip.DiscountEndDate.Value : DBNull.Value);
            cmd.Parameters.AddWithValue("@TotalRooms", trip.TotalRooms);
            cmd.Parameters.AddWithValue("@MinAge", trip.MinAge);
            cmd.Parameters.AddWithValue("@PackageType", trip.PackageType);
            cmd.Parameters.AddWithValue("@Description",
                string.IsNullOrWhiteSpace(trip.Description) ? (object)DBNull.Value : trip.Description);
            cmd.Parameters.AddWithValue("@ImagePath",
                string.IsNullOrWhiteSpace(trip.ImagePath) ? (object)DBNull.Value : trip.ImagePath);
            cmd.Parameters.AddWithValue("@PopularityScore", trip.PopularityScore);
            cmd.Parameters.AddWithValue("@IsActive", trip.IsActive);
        }

        // Map database row to Trip object
        private Trip MapTrip(SqlDataReader reader)
        {
            return new Trip
            {
                TripId = reader.GetInt32(reader.GetOrdinal("TripId")),
                PackageName = reader.GetString(reader.GetOrdinal("PackageName")),
                Destination = reader.GetString(reader.GetOrdinal("Destination")),
                Country = reader.GetString(reader.GetOrdinal("Country")),
                StartDate = reader.GetDateTime(reader.GetOrdinal("StartDate")),
                EndDate = reader.GetDateTime(reader.GetOrdinal("EndDate")),
                BasePrice = reader.GetDecimal(reader.GetOrdinal("BasePrice")),
                DiscountPrice = reader.IsDBNull(reader.GetOrdinal("DiscountPrice")) 
                    ? null : reader.GetDecimal(reader.GetOrdinal("DiscountPrice")),
                DiscountEndDate = reader.IsDBNull(reader.GetOrdinal("DiscountEndDate")) 
                    ? null : reader.GetDateTime(reader.GetOrdinal("DiscountEndDate")),
                TotalRooms = reader.GetInt32(reader.GetOrdinal("TotalRooms")),
                MinAge = reader.GetInt32(reader.GetOrdinal("MinAge")),
                PackageType = reader.GetString(reader.GetOrdinal("PackageType")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) 
                    ? "" : reader.GetString(reader.GetOrdinal("Description")),
                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath")) 
                    ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                PopularityScore = reader.GetInt32(reader.GetOrdinal("PopularityScore")),
                IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
            };
        }
    }
}