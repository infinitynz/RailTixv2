using System;

namespace RailTix.Models.Domain
{
    public class CmsPageComponent
    {
        public Guid Id { get; set; }
        public Guid PageId { get; set; }
        public CmsPage? Page { get; set; }
        public string Type { get; set; } = string.Empty;
        public string SettingsJson { get; set; } = "{}";
        public int Position { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

