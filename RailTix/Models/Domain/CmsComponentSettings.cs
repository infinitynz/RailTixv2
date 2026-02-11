namespace RailTix.Models.Domain
{
    public static class CmsComponentTypes
    {
        public const string Image = "Image";
        public const string Banner = "Banner";
        public const string EventList = "EventList";
    }

    public class CmsImageSettings
    {
        public string? Url { get; set; }
        public string? AltText { get; set; }
        public string? Caption { get; set; }
        public string? Alignment { get; set; }
        public string? LinkUrl { get; set; }
    }

    public class CmsBannerSettings
    {
        public string? Heading { get; set; }
        public string? Subheading { get; set; }
        public string? BackgroundImageUrl { get; set; }
        public string? CtaLabel { get; set; }
        public string? CtaUrl { get; set; }
    }

    public class CmsEventListSettings
    {
        public string? Source { get; set; }
        public string? EventIds { get; set; }
        public string? Category { get; set; }
        public string? Location { get; set; }
        public int? Limit { get; set; }
    }
}

