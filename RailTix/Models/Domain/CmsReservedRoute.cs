using System;

namespace RailTix.Models.Domain
{
    public class CmsReservedRoute
    {
        public Guid Id { get; set; }
        public string Segment { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

