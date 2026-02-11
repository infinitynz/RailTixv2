using RailTix.Models.Domain;

namespace RailTix.Models.ViewModels.Cms
{
    public class CmsComponentViewModel
    {
        public string Type { get; set; } = string.Empty;
        public CmsImageSettings? Image { get; set; }
        public CmsBannerSettings? Banner { get; set; }
        public CmsEventListSettings? EventList { get; set; }
    }
}

