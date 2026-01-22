using Microsoft.AspNetCore.Identity;

namespace RailTix.Models.Domain
{
    public class ApplicationUser : IdentityUser
    {
        // ISO currency code preferred by the user (e.g., NZD, USD)
        public string? PreferredCurrency { get; set; }

        // IANA timezone id (e.g., Pacific/Auckland)
        public string? TimeZoneId { get; set; }

        // BCP-47 language tag (e.g., en-NZ)
        public string? Locale { get; set; }

        // Profile location
        public string? Country { get; set; }
        public string? City { get; set; }
    }
}


