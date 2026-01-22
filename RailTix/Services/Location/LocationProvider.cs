using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using RailTix.Models.Location;
using RailTix.Services.Geo;

namespace RailTix.Services.Location
{
    public class LocationProvider : ILocationProvider
    {
        private const string CookieName = "rtx_loc";
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILocationService _locationService;
        private readonly IGeoIpService _geoIpService;
        private readonly IDataProtector _protector;

        public LocationProvider(
            IHttpContextAccessor httpContextAccessor,
            ILocationService locationService,
            IGeoIpService geoIpService,
            IDataProtectionProvider dataProtectionProvider)
        {
            _httpContextAccessor = httpContextAccessor;
            _locationService = locationService;
            _geoIpService = geoIpService;
            _protector = dataProtectionProvider.CreateProtector("RailTix.LocationCookie.v1");
        }

        public UserLocation? TryGetFromCookie()
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return null;
            if (ctx.Request.Cookies.TryGetValue(CookieName, out var raw))
            {
                try
                {
                    var json = _protector.Unprotect(raw);
                    var loc = JsonSerializer.Deserialize<UserLocation>(json);
                    return loc;
                }
                catch
                {
                    return null;
                }
            }
            return null;
        }

        public async Task<UserLocation?> SetFromCoordinatesAsync(double latitude, double longitude, string source = "browser")
        {
            var rec = _locationService.FindNearest(latitude, longitude);
            if (rec == null) return null;
            var loc = new UserLocation
            {
                Country = rec.Country,
                City = rec.City,
                Latitude = rec.Latitude,
                Longitude = rec.Longitude,
                TimeZoneId = rec.TimeZoneId,
                Currency = rec.Currency,
                Source = source,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            SetCookie(loc);
            return loc;
        }

        public Task<UserLocation?> SetFromCountryCityAsync(string country, string city, string source = "profile")
        {
            var rec = _locationService.GetRecord(country, city);
            if (rec == null) return Task.FromResult<UserLocation?>(null);
            var loc = new UserLocation
            {
                Country = rec.Country,
                City = rec.City,
                Latitude = rec.Latitude,
                Longitude = rec.Longitude,
                TimeZoneId = rec.TimeZoneId,
                Currency = rec.Currency,
                Source = source,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            SetCookie(loc);
            return Task.FromResult<UserLocation?>(loc);
        }

        public async Task<UserLocation?> SetFromIpAsync(string ip, string source = "ip")
        {
            var r = await _geoIpService.LookupAsync(ip);
            if (r == null) return null;
            var (country, city, lat, lng) = r.Value;
            // Normalize to our supported cities
            var rec = _locationService.GetRecord(country, city) ?? _locationService.FindNearest(lat, lng);
            if (rec == null) return null;
            var loc = new UserLocation
            {
                Country = rec.Country,
                City = rec.City,
                Latitude = rec.Latitude,
                Longitude = rec.Longitude,
                TimeZoneId = rec.TimeZoneId,
                Currency = rec.Currency,
                Source = source,
                TimestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            };
            SetCookie(loc);
            return loc;
        }

        private void SetCookie(UserLocation loc)
        {
            var ctx = _httpContextAccessor.HttpContext;
            if (ctx == null) return;
            var json = JsonSerializer.Serialize(loc);
            var protectedVal = _protector.Protect(json);
            ctx.Response.Cookies.Append(CookieName, protectedVal, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(30)
            });
        }
    }
}


