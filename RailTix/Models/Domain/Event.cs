using System;

namespace RailTix.Models.Domain
{
    public class Event
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Slug { get; set; } = string.Empty;
        public string Status { get; set; } = EventStatuses.Draft;
        public DateTime StartsAtLocal { get; set; }
        public DateTime EndsAtLocal { get; set; }
        public string TimeZoneId { get; set; } = "UTC";
        public string CurrencyCode { get; set; } = "USD";
        public string? OrganizerName { get; set; }
        public string? VenueName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
        public string? PostalCode { get; set; }
        public string CreatedByUserId { get; set; } = string.Empty;
        public ApplicationUser? CreatedByUser { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

