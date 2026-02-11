using System;
using System.ComponentModel.DataAnnotations;

namespace RailTix.Models.ViewModels.Events
{
    public class EventEditViewModel
    {
        public Guid? Id { get; set; }

        [Required, StringLength(200)]
        [Display(Name = "Event title")]
        public string Title { get; set; } = string.Empty;

        [StringLength(4000)]
        public string? Description { get; set; }

        [StringLength(200)]
        [Display(Name = "Event slug")]
        public string? Slug { get; set; }

        [Required]
        [Display(Name = "Start date & time")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? StartsAtLocal { get; set; }

        [Required]
        [Display(Name = "End date & time")]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-ddTHH:mm}", ApplyFormatInEditMode = true)]
        public DateTime? EndsAtLocal { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Time zone")]
        public string TimeZoneId { get; set; } = "UTC";

        [Required, StringLength(3, MinimumLength = 3)]
        [Display(Name = "Currency")]
        public string CurrencyCode { get; set; } = "USD";

        [StringLength(200)]
        [Display(Name = "Organizer name")]
        public string? OrganizerName { get; set; }

        [StringLength(200)]
        [Display(Name = "Venue name")]
        public string? VenueName { get; set; }

        [StringLength(200)]
        [Display(Name = "Address line 1")]
        public string? AddressLine1 { get; set; }

        [StringLength(200)]
        [Display(Name = "Address line 2")]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? City { get; set; }

        [StringLength(100)]
        public string? Region { get; set; }

        [StringLength(100)]
        public string? Country { get; set; }

        [StringLength(20)]
        [Display(Name = "Postal code")]
        public string? PostalCode { get; set; }
    }
}

