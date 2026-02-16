using System;
using System.Collections.Generic;

namespace RailTix.Models.ViewModels.Account
{
    public class StripeConnectionStatusViewModel
    {
        public bool IsStripeConfigured { get; set; }
        public string? StripeAccountId { get; set; }
        public string? StripeAccountType { get; set; }
        public bool ChargesEnabled { get; set; }
        public bool PayoutsEnabled { get; set; }
        public string? ErrorMessage { get; set; }
        public string? RequirementsDisabledReason { get; set; }
        public IReadOnlyList<string> CapabilityIssues { get; set; } = Array.Empty<string>();
        public IReadOnlyList<string> MissingRequirements { get; set; } = Array.Empty<string>();

        public bool HasStripeAccountId => !string.IsNullOrWhiteSpace(StripeAccountId);
        public bool IsSetupComplete => ChargesEnabled && PayoutsEnabled;

        public string StateLabel
        {
            get
            {
                if (!IsStripeConfigured)
                {
                    return "Not Configured";
                }

                if (!HasStripeAccountId)
                {
                    return "Not Connected";
                }

                return IsSetupComplete ? "Connected" : "Setup Incomplete";
            }
        }
    }
}


