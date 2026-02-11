using System.Collections.Generic;

namespace RailTix.Models.ViewModels.Cms.Admin
{
    public class CmsPageListViewModel
    {
        public List<CmsPageTreeItemViewModel> Pages { get; set; } = new List<CmsPageTreeItemViewModel>();
    }
}

