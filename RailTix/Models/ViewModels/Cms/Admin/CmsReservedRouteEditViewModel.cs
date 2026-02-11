using System;
using System.ComponentModel.DataAnnotations;

namespace RailTix.Models.ViewModels.Cms.Admin
{
    public class CmsReservedRouteEditViewModel
    {
        public Guid? Id { get; set; }

        [Required, StringLength(200)]
        public string Segment { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}

