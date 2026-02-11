using System;

namespace RailTix.Models.ViewModels.Cms.Admin
{
    public class CmsComponentListItemViewModel
    {
        public Guid Id { get; set; }
        public string Type { get; set; } = string.Empty;
        public int Position { get; set; }
        public bool IsEnabled { get; set; }
    }
}

