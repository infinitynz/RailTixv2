using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace RailTix.Models.ViewModels.Cms.Admin
{
    public class CmsPageEditViewModel
    {
        public Guid? Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Slug { get; set; }

        [StringLength(512)]
        public string? CustomUrl { get; set; }

        public Guid? ParentId { get; set; }

        public int Position { get; set; }

        public bool IsHomepage { get; set; }

        public bool IsPublished { get; set; }

        public string? CurrentPath { get; set; }

        public List<SelectListItem> ParentOptions { get; set; } = new List<SelectListItem>();

        public List<CmsComponentListItemViewModel> Components { get; set; } = new List<CmsComponentListItemViewModel>();
    }
}

