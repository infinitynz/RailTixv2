using System.Threading.Tasks;

namespace RailTix.Services.Geo
{
    public interface IGeoIpService
    {
        Task<(string Country, string City, double Latitude, double Longitude)?> LookupAsync(string ip);
    }
}


