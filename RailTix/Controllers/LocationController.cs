using Microsoft.AspNetCore.Mvc;
using RailTix.Services.Location;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace RailTix.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LocationController : ControllerBase
    {
        private readonly ILocationService _locationService;
        private readonly ILocationProvider _locationProvider;
        public LocationController(ILocationService locationService, ILocationProvider locationProvider)
        {
            _locationService = locationService;
            _locationProvider = locationProvider;
        }

        [HttpGet("cities")]
        public IActionResult Cities([FromQuery] string country)
        {
            if (string.IsNullOrWhiteSpace(country))
            {
                return BadRequest(new { error = "country is required" });
            }
            var cities = _locationService.GetCities(country).ToArray();
            return Ok(cities);
        }

        public sealed class UpdateBody
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }

        [HttpPost("update")]
        public async Task<IActionResult> Update([FromBody] UpdateBody body)
        {
            var loc = await _locationProvider.SetFromCoordinatesAsync(body.Latitude, body.Longitude, "browser");
            if (loc == null) return BadRequest(new { error = "unable_to_map" });
            return Ok(loc);
        }

        [HttpGet("guess")]
        public async Task<IActionResult> Guess()
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
            if (string.IsNullOrEmpty(ip)) return BadRequest(new { error = "no_ip" });
            var loc = await _locationProvider.SetFromIpAsync(ip, "ip");
            if (loc == null) return NotFound(new { error = "unresolved" });
            return Ok(loc);
        }

        [HttpGet("current")]
        public IActionResult Current()
        {
            var loc = _locationProvider.TryGetFromCookie();
            if (loc == null) return NotFound();
            return Ok(loc);
        }
    }
}


