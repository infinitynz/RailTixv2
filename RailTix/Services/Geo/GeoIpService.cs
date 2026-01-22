using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace RailTix.Services.Geo
{
    public class GeoIpService : IGeoIpService
    {
        private readonly HttpClient _httpClient;
        public GeoIpService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<(string Country, string City, double Latitude, double Longitude)?> LookupAsync(string ip)
        {
            // Using ipwho.is (free; no key). Example: https://ipwho.is/8.8.8.8?fields=success,country,city,latitude,longitude
            var url = $"https://ipwho.is/{ip}?fields=success,country,city,latitude,longitude";
            var resp = await _httpClient.GetAsync(url);
            if (!resp.IsSuccessStatusCode) return null;
            var stream = await resp.Content.ReadAsStreamAsync();
            var obj = await JsonSerializer.DeserializeAsync<IpWhoResponse>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            if (obj?.Success != true) return null;
            return (obj.Country ?? "", obj.City ?? "", obj.Latitude, obj.Longitude);
        }

        private sealed class IpWhoResponse
        {
            public bool Success { get; set; }
            public string? Country { get; set; }
            public string? City { get; set; }
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}


