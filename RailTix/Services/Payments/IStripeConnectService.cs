using RailTix.Models.Domain;
using System.Threading;
using System.Threading.Tasks;

namespace RailTix.Services.Payments
{
    public interface IStripeConnectService
    {
        Task<StripeConnectionStatus> GetStatusAsync(ApplicationUser user, CancellationToken cancellationToken = default);
        Task<StripeConnectionStatus> GetPlatformStatusAsync(CancellationToken cancellationToken = default);
        Task<string> CreateOrGetOnboardingLinkAsync(
            ApplicationUser user,
            string returnUrl,
            string refreshUrl,
            CancellationToken cancellationToken = default);
        Task<string?> CreateDashboardLoginLinkAsync(ApplicationUser user, CancellationToken cancellationToken = default);
    }
}


