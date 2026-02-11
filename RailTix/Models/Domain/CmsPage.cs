using System;
using System.Collections.Generic;

namespace RailTix.Models.Domain
{
    public class CmsPage
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public Guid? ParentId { get; set; }
        public CmsPage? Parent { get; set; }
        public ICollection<CmsPage> Children { get; set; } = new List<CmsPage>();
        public ICollection<CmsPageComponent> Components { get; set; } = new List<CmsPageComponent>();
        public int Position { get; set; }
        public bool IsHomepage { get; set; }
        public bool IsPublished { get; set; }
        public string? CustomUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

