namespace RailTix.Models.Options
{
    public class StripeOptions
    {
        public string SecretKey { get; set; } = "";
        public string PublishableKey { get; set; } = "";
        public decimal PlatformFeePercent { get; set; } = 2m;
    }
}


