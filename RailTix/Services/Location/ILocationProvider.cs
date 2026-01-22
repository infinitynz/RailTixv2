using System.Threading.Tasks;
using RailTix.Models.Location;

namespace RailTix.Services.Location
{
    public interface ILocationProvider
    {
        UserLocation? TryGetFromCookie();
        Task<UserLocation?> SetFromCoordinatesAsync(double latitude, double longitude, string source = "browser");
        Task<UserLocation?> SetFromCountryCityAsync(string country, string city, string source = "profile");
        Task<UserLocation?> SetFromIpAsync(string ip, string source = "ip");
    }
}


