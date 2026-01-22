using System.Collections.Generic;
using System.Linq;

namespace RailTix.Services.Location
{
    public class LocationService : ILocationService
    {
        private static readonly List<CityRecord> Cities = new List<CityRecord>
        {
            new CityRecord { Country = "New Zealand", City = "Auckland", Latitude = -36.8485, Longitude = 174.7633, TimeZoneId = "Pacific/Auckland", Currency = "NZD" },
            new CityRecord { Country = "New Zealand", City = "Wellington", Latitude = -41.2865, Longitude = 174.7762, TimeZoneId = "Pacific/Auckland", Currency = "NZD" },
            new CityRecord { Country = "New Zealand", City = "Christchurch", Latitude = -43.5320, Longitude = 172.6306, TimeZoneId = "Pacific/Auckland", Currency = "NZD" },
            new CityRecord { Country = "New Zealand", City = "Hamilton", Latitude = -37.7870, Longitude = 175.2793, TimeZoneId = "Pacific/Auckland", Currency = "NZD" },
            new CityRecord { Country = "New Zealand", City = "Tauranga", Latitude = -37.6878, Longitude = 176.1651, TimeZoneId = "Pacific/Auckland", Currency = "NZD" },
            new CityRecord { Country = "New Zealand", City = "Dunedin", Latitude = -45.8788, Longitude = 170.5028, TimeZoneId = "Pacific/Auckland", Currency = "NZD" },
            new CityRecord { Country = "Australia", City = "Sydney", Latitude = -33.8688, Longitude = 151.2093, TimeZoneId = "Australia/Sydney", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Melbourne", Latitude = -37.8136, Longitude = 144.9631, TimeZoneId = "Australia/Melbourne", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Brisbane", Latitude = -27.4698, Longitude = 153.0251, TimeZoneId = "Australia/Brisbane", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Perth", Latitude = -31.9523, Longitude = 115.8613, TimeZoneId = "Australia/Perth", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Adelaide", Latitude = -34.9285, Longitude = 138.6007, TimeZoneId = "Australia/Adelaide", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Canberra", Latitude = -35.2809, Longitude = 149.1300, TimeZoneId = "Australia/Sydney", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Hobart", Latitude = -42.8821, Longitude = 147.3272, TimeZoneId = "Australia/Hobart", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Gold Coast", Latitude = -28.0167, Longitude = 153.4000, TimeZoneId = "Australia/Brisbane", Currency = "AUD" },
            new CityRecord { Country = "Australia", City = "Newcastle", Latitude = -32.9283, Longitude = 151.7817, TimeZoneId = "Australia/Sydney", Currency = "AUD" }
        };

        private static readonly Dictionary<string, string> CountryCurrency =
            new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
            {
                ["New Zealand"] = "NZD",
                ["Australia"] = "AUD"
            };

        public IEnumerable<string> GetCountries()
        {
            return Cities.Select(c => c.Country).Distinct().OrderBy(k => k);
        }

        public IEnumerable<string> GetCities(string country)
        {
            return Cities.Where(c => string.Equals(c.Country, country, System.StringComparison.OrdinalIgnoreCase))
                .Select(c => c.City)
                .Distinct()
                .OrderBy(c => c);
        }

        public string? ResolveCurrency(string country)
        {
            if (CountryCurrency.TryGetValue(country, out var c))
            {
                return c;
            }
            return null;
        }

        public string? ResolveTimeZone(string country, string city)
        {
            return Cities.FirstOrDefault(r =>
                string.Equals(r.Country, country, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.City, city, System.StringComparison.OrdinalIgnoreCase))?.TimeZoneId;
        }

        public CityRecord? GetRecord(string country, string city)
        {
            return Cities.FirstOrDefault(r =>
                string.Equals(r.Country, country, System.StringComparison.OrdinalIgnoreCase) &&
                string.Equals(r.City, city, System.StringComparison.OrdinalIgnoreCase));
        }

        public CityRecord? FindNearest(double latitude, double longitude)
        {
            CityRecord? best = null;
            double bestDist = double.MaxValue;
            foreach (var r in Cities)
            {
                var dist = Haversine(latitude, longitude, r.Latitude, r.Longitude);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = r;
                }
            }
            return best;
        }

        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            const double R = 6371e3; // metres
            double phi1 = lat1 * System.Math.PI / 180.0;
            double phi2 = lat2 * System.Math.PI / 180.0;
            double dphi = (lat2 - lat1) * System.Math.PI / 180.0;
            double dlambda = (lon2 - lon1) * System.Math.PI / 180.0;

            double a = System.Math.Sin(dphi / 2) * System.Math.Sin(dphi / 2) +
                       System.Math.Cos(phi1) * System.Math.Cos(phi2) *
                       System.Math.Sin(dlambda / 2) * System.Math.Sin(dlambda / 2);
            double c = 2 * System.Math.Atan2(System.Math.Sqrt(a), System.Math.Sqrt(1 - a));
            return R * c;
        }
    }
}


