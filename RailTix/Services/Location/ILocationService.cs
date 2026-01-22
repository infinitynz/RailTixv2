using System.Collections.Generic;

namespace RailTix.Services.Location
{
    public interface ILocationService
    {
        IEnumerable<string> GetCountries();
        IEnumerable<string> GetCities(string country);
        string? ResolveCurrency(string country);
        string? ResolveTimeZone(string country, string city);
        CityRecord? GetRecord(string country, string city);
        CityRecord? FindNearest(double latitude, double longitude);
    }

    public class CityRecord
    {
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZoneId { get; set; } = "";
        public string Currency { get; set; } = "";
    }
}


