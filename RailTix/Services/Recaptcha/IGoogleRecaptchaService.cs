using System.Threading.Tasks;

namespace RailTix.Services.Recaptcha
{
    public interface IGoogleRecaptchaService
    {
        Task<bool> VerifyAsync(string token, string? remoteIp);
        Task<bool> VerifyAsync(string token, string? remoteIp, string action, double? minimumScore = null);
    }
}


