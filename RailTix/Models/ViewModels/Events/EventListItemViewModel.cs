using System;

namespace RailTix.Models.ViewModels.Events
{
    public class EventListItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartsAtLocal { get; set; }
        public DateTime EndsAtLocal { get; set; }
        public string TimeZoneId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}

