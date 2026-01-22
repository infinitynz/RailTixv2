namespace RailTix.Models.Options
{
    public class GoogleRecaptchaOptions
    {
        public string SiteKey { get; set; } = "";
        public string SecretKey { get; set; } = "";
        public double MinimumScore { get; set; } = 0.5;
    }
}


