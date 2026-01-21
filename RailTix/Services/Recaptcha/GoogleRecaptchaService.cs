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
            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

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

            return obj?.Success ?? false;
        }

        private sealed class RecaptchaVerifyResponse
        {
            public bool Success { get; set; }
        }
    }
}


