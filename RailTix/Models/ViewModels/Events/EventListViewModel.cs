using System.Collections.Generic;

namespace RailTix.Models.ViewModels.Events
{
    public class EventListViewModel
    {
        public IReadOnlyList<EventListItemViewModel> Events { get; set; } = new List<EventListItemViewModel>();
        public bool IsAdmin { get; set; }
        public bool CanCreateEvents { get; set; }
        public string? CreateEventsBlockedReason { get; set; }
    }
}

