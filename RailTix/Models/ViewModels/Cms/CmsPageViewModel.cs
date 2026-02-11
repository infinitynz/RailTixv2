using System.Collections.Generic;

namespace RailTix.Models.ViewModels.Cms
{
    public class CmsPageViewModel
    {
        public string Title { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsHomepage { get; set; }
        public List<CmsComponentViewModel> Components { get; set; } = new List<CmsComponentViewModel>();
    }
}

