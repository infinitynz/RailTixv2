namespace RailTix.Models.ViewModels.Account
{
    public class AccountManageViewModel
    {
        public bool IsAdmin { get; set; }
        public bool IsEventManager { get; set; }
        public bool CanManageEvents => IsAdmin || IsEventManager;
        public bool HasAnyEvents { get; set; }
        public StripeConnectionStatusViewModel? StripeStatus { get; set; }
    }
}


