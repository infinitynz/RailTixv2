using System;
using System.Collections.Generic;

namespace RailTix.Models.ViewModels.Cms.Admin
{
    public class CmsPageTreeItemViewModel
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsHomepage { get; set; }
        public bool IsPublished { get; set; }
        public int Position { get; set; }
        public List<CmsPageTreeItemViewModel> Children { get; set; } = new List<CmsPageTreeItemViewModel>();
    }
}

