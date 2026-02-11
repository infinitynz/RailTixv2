using System.Collections.Generic;

namespace RailTix.Models.ViewModels.Cms.Admin
{
    public class CmsReservedRouteListViewModel
    {
        public List<CmsReservedRouteEditViewModel> Routes { get; set; } = new List<CmsReservedRouteEditViewModel>();
    }
}

