using System;

namespace MicroShift.Helpers
{
    public static class GeoCalculator
    {
        /// <summary>
        /// Calculates the distance between two points on Earth using the Haversine formula.
        /// </summary>
        public static double GetDistanceInKm(double lat1, double lon1, double lat2, double lon2)
        {
            // Earth's radius in kilometers
            const double EarthRadiusKm = 6371.0;

            // Convert degrees to radians
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);

            double rLat1 = ToRadians(lat1);
            double rLat2 = ToRadians(lat2);

            // Haversine formula math
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(rLat1) * Math.Cos(rLat2);

            double c = 2 * Math.Asin(Math.Sqrt(a));

            // Return distance rounded to 1 decimal point (e.g., 2.4 km)
            return Math.Round(EarthRadiusKm * c, 1);
        }

        private static double ToRadians(double val)
        {
            return (Math.PI / 180) * val;
        }
    }
}