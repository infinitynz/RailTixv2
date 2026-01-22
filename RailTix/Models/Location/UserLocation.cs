namespace RailTix.Models.Location
{
    public class UserLocation
    {
        public string Country { get; set; } = "";
        public string City { get; set; } = "";
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string TimeZoneId { get; set; } = "";
        public string Currency { get; set; } = "";
        public string Source { get; set; } = ""; // profile|cookie|browser|ip|default
        public long TimestampUnix { get; set; }
    }
}


