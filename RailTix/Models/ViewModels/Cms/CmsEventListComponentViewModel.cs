using System;
using System.Collections.Generic;

namespace RailTix.Models.ViewModels.Cms
{
    public class CmsEventListComponentViewModel
    {
        public string Source { get; set; } = "query";
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int Limit { get; set; } = 10;
        public List<CmsEventListItemViewModel> Events { get; set; } = new List<CmsEventListItemViewModel>();
    }

    public class CmsEventListItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public DateTime StartsAtLocal { get; set; }
        public DateTime EndsAtLocal { get; set; }
        public string? VenueName { get; set; }
        public string? City { get; set; }
        public string? Region { get; set; }
        public string? Country { get; set; }
    }
}

