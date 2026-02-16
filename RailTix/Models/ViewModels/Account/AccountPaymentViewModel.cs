namespace RailTix.Models.ViewModels.Account
{
    public class AccountPaymentViewModel
    {
        public bool IsAdmin { get; set; }
        public bool IsEventManager { get; set; }
        public bool UsesPlatformStripeAccount { get; set; }
        public string StripeEnvironmentLabel { get; set; } = "Unknown";
        public string? ReturnUrl { get; set; }
        public decimal PlatformFeePercent { get; set; }
        public StripeConnectionStatusViewModel StripeStatus { get; set; } = new StripeConnectionStatusViewModel();
    }
}


