using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using RailTix.Models.Options;

namespace RailTix.Services.Recaptcha
{
    public class GoogleRecaptchaService : IGoogleRecaptchaService
    {
        private readonly HttpClient _httpClient;
        private readonly GoogleRecaptchaOptions _options;

        public GoogleRecaptchaService(HttpClient httpClient, IOptions<GoogleRecaptchaOptions> options)
        {
            _httpClient = httpClient;
            _options = options.Value;
        }

        public async Task<bool> VerifyAsync(string token, string? remoteIp)
        {
            return await VerifyAsync(token, remoteIp, action: string.Empty, minimumScore: _options.MinimumScore);
        }

        public async Task<bool> VerifyAsync(string token, string? remoteIp, string action, double? minimumScore = null)
        {
            if (string.IsNullOrWhiteSpace(token)) return false;

            var content = new StringContent(
                $"secret={_options.SecretKey}&response={token}&remoteip={remoteIp}",
                Encoding.UTF8,
                "application/x-www-form-urlencoded");

            using var response = await _httpClient.PostAsync("https://www.google.com/recaptcha/api/siteverify", content);
            response.EnsureSuccessStatusCode();
            var stream = await response.Content.ReadAsStreamAsync();

            var obj = await JsonSerializer.DeserializeAsync<RecaptchaVerifyResponse>(stream, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (obj is null || !obj.Success) return false;
            if (!string.IsNullOrWhiteSpace(action) && !string.Equals(obj.Action, action, System.StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            var threshold = minimumScore ?? _options.MinimumScore;
            if (obj.Score < threshold) return false;
            return true;
        }

        private sealed class RecaptchaVerifyResponse
        {
            public bool Success { get; set; }
            public double Score { get; set; }
            public string? Action { get; set; }
        }
    }
}


