using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RailTix.Models.Domain;
using RailTix.Models.Options;

namespace RailTix.Services.Payments
{
    public class StripeConnectService : IStripeConnectService
    {
        private const string StripeApiClientName = "StripeApi";
        private const string StripeLoginProvider = "RailTix.Stripe";
        private const string StripeAccountIdTokenName = "ConnectAccountId";
        private static readonly string[] RequiredCapabilities = { "card_payments", "transfers" };

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly StripeOptions _stripeOptions;
        private readonly ILogger<StripeConnectService> _logger;

        public StripeConnectService(
            UserManager<ApplicationUser> userManager,
            IHttpClientFactory httpClientFactory,
            IOptions<StripeOptions> stripeOptions,
            ILogger<StripeConnectService> logger)
        {
            _userManager = userManager;
            _httpClientFactory = httpClientFactory;
            _stripeOptions = stripeOptions.Value;
            _logger = logger;
        }

        public async Task<StripeConnectionStatus> GetStatusAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            var status = new StripeConnectionStatus
            {
                IsStripeConfigured = IsStripeConfigured()
            };

            if (!status.IsStripeConfigured)
            {
                status.ErrorMessage = "Stripe is not configured for this environment.";
                return status;
            }

            var accountId = await _userManager.GetAuthenticationTokenAsync(user, StripeLoginProvider, StripeAccountIdTokenName);
            status.StripeAccountId = accountId;
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return status;
            }

            try
            {
                var account = await GetStripeAccountAsync(accountId, cancellationToken);
                status.StripeAccountType = GetString(account, "type");
                status.ChargesEnabled = GetBool(account, "charges_enabled");
                status.PayoutsEnabled = GetBool(account, "payouts_enabled");
                if (!status.IsSetupComplete)
                {
                    await PopulateCapabilityRequirementsAsync(status, accountId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve Stripe Connect status for user {UserId}", user.Id);
                status.ErrorMessage = "Unable to verify Stripe status right now. Please try again.";
            }

            return status;
        }

        public async Task<StripeConnectionStatus> GetPlatformStatusAsync(CancellationToken cancellationToken = default)
        {
            var status = new StripeConnectionStatus
            {
                IsStripeConfigured = IsStripeConfigured()
            };

            if (!status.IsStripeConfigured)
            {
                status.ErrorMessage = "Stripe is not configured for this environment.";
                return status;
            }

            try
            {
                var account = await SendStripeRequestAsync(
                    HttpMethod.Get,
                    "v1/account",
                    formFields: null,
                    cancellationToken);
                status.StripeAccountId = GetString(account, "id");
                status.StripeAccountType = GetString(account, "type");
                status.ChargesEnabled = GetBool(account, "charges_enabled");
                status.PayoutsEnabled = GetBool(account, "payouts_enabled");
                if (!status.IsSetupComplete && !string.IsNullOrWhiteSpace(status.StripeAccountId))
                {
                    await PopulateCapabilityRequirementsAsync(status, status.StripeAccountId, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve Stripe platform status.");
                status.ErrorMessage = "Unable to verify platform Stripe status right now. Please try again.";
            }

            return status;
        }

        public async Task<string> CreateOrGetOnboardingLinkAsync(
            ApplicationUser user,
            string returnUrl,
            string refreshUrl,
            CancellationToken cancellationToken = default)
        {
            if (!IsStripeConfigured())
            {
                throw new InvalidOperationException("Stripe is not configured. Add Stripe:SecretKey in app settings.");
            }

            var accountId = await _userManager.GetAuthenticationTokenAsync(user, StripeLoginProvider, StripeAccountIdTokenName);
            if (!string.IsNullOrWhiteSpace(accountId))
            {
                try
                {
                    await GetStripeAccountAsync(accountId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Existing Stripe account id for user {UserId} is invalid; creating a new one.", user.Id);
                    accountId = null;
                }
            }

            if (string.IsNullOrWhiteSpace(accountId))
            {
                accountId = await CreateExpressAccountAsync(user, cancellationToken);
                await _userManager.SetAuthenticationTokenAsync(user, StripeLoginProvider, StripeAccountIdTokenName, accountId);
            }

            var accountLink = await CreateAccountLinkAsync(accountId, returnUrl, refreshUrl, cancellationToken);
            var url = GetString(accountLink, "url");
            if (string.IsNullOrWhiteSpace(url))
            {
                throw new InvalidOperationException("Stripe did not return an onboarding URL.");
            }

            return url;
        }

        public async Task<string?> CreateDashboardLoginLinkAsync(ApplicationUser user, CancellationToken cancellationToken = default)
        {
            if (!IsStripeConfigured())
            {
                return null;
            }

            var accountId = await _userManager.GetAuthenticationTokenAsync(user, StripeLoginProvider, StripeAccountIdTokenName);
            if (string.IsNullOrWhiteSpace(accountId))
            {
                return null;
            }

            try
            {
                var loginLink = await SendStripeRequestAsync(
                    HttpMethod.Post,
                    $"v1/accounts/{Uri.EscapeDataString(accountId)}/login_links",
                    formFields: null,
                    cancellationToken);
                return GetString(loginLink, "url");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create Stripe login link for user {UserId}", user.Id);
                return null;
            }
        }

        private bool IsStripeConfigured()
        {
            return !string.IsNullOrWhiteSpace(_stripeOptions.SecretKey);
        }

        private async Task<string> CreateExpressAccountAsync(ApplicationUser user, CancellationToken cancellationToken)
        {
            var fields = new Dictionary<string, string?>
            {
                ["type"] = "express",
                ["email"] = user.Email ?? string.Empty,
                ["metadata[userId]"] = user.Id,
                ["metadata[role]"] = await ResolveStripeRoleAsync(user)
            };

            var account = await SendStripeRequestAsync(HttpMethod.Post, "v1/accounts", fields, cancellationToken);
            var id = GetString(account, "id");
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new InvalidOperationException("Stripe account creation succeeded but no account id was returned.");
            }

            return id;
        }

        private async Task<string> ResolveStripeRoleAsync(ApplicationUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);
            if (roles.Contains("Admin"))
            {
                return "Admin";
            }

            if (roles.Contains("EventManager"))
            {
                return "EventManager";
            }

            return "User";
        }

        private async Task<JsonElement> GetStripeAccountAsync(string accountId, CancellationToken cancellationToken)
        {
            return await SendStripeRequestAsync(
                HttpMethod.Get,
                $"v1/accounts/{Uri.EscapeDataString(accountId)}",
                formFields: null,
                cancellationToken);
        }

        private async Task<JsonElement> CreateAccountLinkAsync(
            string accountId,
            string returnUrl,
            string refreshUrl,
            CancellationToken cancellationToken)
        {
            var fields = new Dictionary<string, string?>
            {
                ["account"] = accountId,
                ["refresh_url"] = refreshUrl,
                ["return_url"] = returnUrl,
                ["type"] = "account_onboarding"
            };

            return await SendStripeRequestAsync(HttpMethod.Post, "v1/account_links", fields, cancellationToken);
        }

        private async Task PopulateCapabilityRequirementsAsync(
            StripeConnectionStatus status,
            string accountId,
            CancellationToken cancellationToken)
        {
            try
            {
                var capabilities = await SendStripeRequestAsync(
                    HttpMethod.Get,
                    $"v1/accounts/{Uri.EscapeDataString(accountId)}/capabilities",
                    formFields: null,
                    cancellationToken);

                var capabilityIssues = new List<string>();
                var missingRequirements = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                var disabledReasons = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var capabilityId in RequiredCapabilities)
                {
                    if (!TryGetCapability(capabilities, capabilityId, out var capability))
                    {
                        capabilityIssues.Add($"{capabilityId}: not returned by Stripe");
                        continue;
                    }

                    var capabilityStatus = GetString(capability, "status") ?? "unknown";
                    var requested = GetBool(capability, "requested");
                    var disabledReason = GetNestedString(capability, "requirements", "disabled_reason");

                    if (!string.Equals(capabilityStatus, "active", StringComparison.OrdinalIgnoreCase))
                    {
                        var issue = $"{capabilityId}: {capabilityStatus}";
                        if (!requested)
                        {
                            issue += " (not requested)";
                        }

                        if (!string.IsNullOrWhiteSpace(disabledReason))
                        {
                            issue += $" [{disabledReason}]";
                            disabledReasons.Add(disabledReason);
                        }

                        capabilityIssues.Add(issue);
                    }

                    AddNestedStringArrayValues(capability, missingRequirements, "requirements", "currently_due");
                    AddNestedStringArrayValues(capability, missingRequirements, "requirements", "past_due");
                }

                status.CapabilityIssues = capabilityIssues;
                status.MissingRequirements = missingRequirements
                    .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                status.RequirementsDisabledReason = disabledReasons.Count == 0
                    ? null
                    : string.Join(", ", disabledReasons.OrderBy(value => value, StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve Stripe capability requirements for account {StripeAccountId}", accountId);
            }
        }

        private async Task<JsonElement> SendStripeRequestAsync(
            HttpMethod method,
            string path,
            IDictionary<string, string?>? formFields,
            CancellationToken cancellationToken)
        {
            var client = _httpClientFactory.CreateClient(StripeApiClientName);

            using var request = new HttpRequestMessage(method, path);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _stripeOptions.SecretKey);

            if (method != HttpMethod.Get && formFields != null)
            {
                var payload = new List<KeyValuePair<string, string>>();
                foreach (var pair in formFields)
                {
                    payload.Add(new KeyValuePair<string, string>(pair.Key, pair.Value ?? string.Empty));
                }

                request.Content = new FormUrlEncodedContent(payload);
            }

            using var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Stripe API call failed ({StatusCode}) for {Path}: {Body}", (int)response.StatusCode, path, content);
                throw new InvalidOperationException($"Stripe request failed for '{path}'.");
            }

            using var document = JsonDocument.Parse(content);
            return document.RootElement.Clone();
        }

        private static string? GetString(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var property) &&
                property.ValueKind == JsonValueKind.String)
            {
                return property.GetString();
            }

            return null;
        }

        private static string? GetNestedString(
            JsonElement element,
            string nestedObjectPropertyName,
            string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(nestedObjectPropertyName, out var nestedObject) &&
                nestedObject.ValueKind == JsonValueKind.Object)
            {
                return GetString(nestedObject, propertyName);
            }

            return null;
        }

        private static bool TryGetCapability(JsonElement capabilitiesRoot, string capabilityId, out JsonElement capability)
        {
            capability = default;

            if (capabilitiesRoot.ValueKind != JsonValueKind.Object ||
                !capabilitiesRoot.TryGetProperty("data", out var data) ||
                data.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var item in data.EnumerateArray())
            {
                var id = GetString(item, "id");
                if (string.Equals(id, capabilityId, StringComparison.OrdinalIgnoreCase))
                {
                    capability = item.Clone();
                    return true;
                }
            }

            return false;
        }

        private static void AddNestedStringArrayValues(
            JsonElement element,
            ISet<string> destination,
            string nestedObjectPropertyName,
            string arrayPropertyName)
        {
            if (element.ValueKind != JsonValueKind.Object ||
                !element.TryGetProperty(nestedObjectPropertyName, out var nestedObject) ||
                nestedObject.ValueKind != JsonValueKind.Object ||
                !nestedObject.TryGetProperty(arrayPropertyName, out var values) ||
                values.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var value in values.EnumerateArray())
            {
                if (value.ValueKind == JsonValueKind.String)
                {
                    var item = value.GetString();
                    if (!string.IsNullOrWhiteSpace(item))
                    {
                        destination.Add(item);
                    }
                }
            }
        }

        private static bool GetBool(JsonElement element, string propertyName)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(propertyName, out var property) &&
                (property.ValueKind == JsonValueKind.True || property.ValueKind == JsonValueKind.False))
            {
                return property.GetBoolean();
            }

            return false;
        }
    }
}


