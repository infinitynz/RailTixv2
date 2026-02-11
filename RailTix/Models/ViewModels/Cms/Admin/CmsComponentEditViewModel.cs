using System;
using System.ComponentModel.DataAnnotations;

namespace RailTix.Models.ViewModels.Cms.Admin
{
    public class CmsComponentEditViewModel
    {
        public Guid? Id { get; set; }
        public Guid PageId { get; set; }

        [Required]
        public string Type { get; set; } = string.Empty;

        public int Position { get; set; }
        public bool IsEnabled { get; set; } = true;

        [Display(Name = "Image URL")]
        public string? ImageUrl { get; set; }
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public string? Alignment { get; set; }
        [Display(Name = "Link URL")]
        public string? LinkUrl { get; set; }

        public string? Heading { get; set; }
        public string? Subheading { get; set; }
        [Display(Name = "Background Image URL")]
        public string? BackgroundImageUrl { get; set; }
        [Display(Name = "CTA Label")]
        public string? CtaLabel { get; set; }
        [Display(Name = "CTA URL")]
        public string? CtaUrl { get; set; }

        public string? Source { get; set; }
        [Display(Name = "Event IDs (comma separated)")]
        public string? EventIds { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int? Limit { get; set; }
    }
}

