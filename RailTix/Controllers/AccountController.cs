using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RailTix.Data;
using RailTix.Models.Domain;
using RailTix.Models.Options;
using RailTix.Services.Recaptcha;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Linq;
using System.Collections.Generic;
using RailTix.Services.Location;
using RailTix.Models.ViewModels.Account;
using RailTix.Services.Payments;

namespace RailTix.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IGoogleRecaptchaService _recaptchaService;
        private readonly GoogleRecaptchaOptions _recaptchaOptions;
        private readonly ILocationService _locationService;
        private readonly ILocationProvider _locationProvider;
        private readonly IStripeConnectService _stripeConnectService;
        private readonly StripeOptions _stripeOptions;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IGoogleRecaptchaService recaptchaService,
            IOptions<GoogleRecaptchaOptions> recaptchaOptions,
            ILocationService locationService,
            ILocationProvider locationProvider,
            IStripeConnectService stripeConnectService,
            IOptions<StripeOptions> stripeOptions,
            ILogger<AccountController> logger)
        {
            _db = db;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _recaptchaService = recaptchaService;
            _recaptchaOptions = recaptchaOptions.Value;
            _locationService = locationService;
            _locationProvider = locationProvider;
            _stripeConnectService = stripeConnectService;
            _stripeOptions = stripeOptions.Value;
            _logger = logger;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            _logger.LogInformation("GET /Account/Login returnUrl={ReturnUrl}", returnUrl);
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = Request.Form["g-recaptcha-response"].ToString();
            var ok = await _recaptchaService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), "login");
            if (!ok)
            {
                _logger.LogWarning("Login reCAPTCHA failed for {Email} from {IP}", model.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed.");
                return View(model);
            }

            var userForLogin = await _userManager.FindByEmailAsync(model.Email);
            if (userForLogin == null)
            {
                _logger.LogWarning("Login failed: unknown email {Email}", model.Email);
                ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                return View(model);
            }

            // If email is not confirmed, show specific message with ability to resend
            if (!await _userManager.IsEmailConfirmedAsync(userForLogin))
            {
                _logger.LogInformation("Login blocked for unconfirmed email {Email}", model.Email);
                ViewBag.EmailNotConfirmed = true;
                ViewBag.PendingEmail = model.Email;
                ModelState.AddModelError(string.Empty, "Your email address is not yet verified.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(userForLogin, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("Login success for {UserId}", userForLogin.Id);
                var u = await _userManager.FindByEmailAsync(model.Email);
                if (u != null && !string.IsNullOrEmpty(u.Country) && !string.IsNullOrEmpty(u.City))
                {
                    await _locationProvider.SetFromCountryCityAsync(u.Country!, u.City!, "login_profile");
                }
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                // future 2FA flow
                _logger.LogInformation("Login requires 2FA for {UserId}", userForLogin.Id);
                ModelState.AddModelError(string.Empty, "Two-factor authentication is required.");
                return View(model);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("Login locked out for {UserId}", userForLogin.Id);
                return RedirectToAction(nameof(Lockout));
            }

            _logger.LogWarning("Login failed for {UserId}", userForLogin.Id);
            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            ViewBag.ReturnUrl = returnUrl;
            var defaultCountry = "New Zealand";
            var defaultCity = "Auckland";
            var vm = new RegisterViewModel
            {
                CountryOptions = _locationService.GetCountries().Select(c => new SelectListItem { Text = c, Value = c, Selected = c == defaultCountry }).ToList(),
                CityOptions = _locationService.GetCities(defaultCountry).Select(c => new SelectListItem { Text = c, Value = c, Selected = c == defaultCity }).ToList(),
                Country = defaultCountry,
                City = defaultCity
            };
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
            {
                model.CountryOptions = _locationService.GetCountries().Select(c => new SelectListItem { Text = c, Value = c, Selected = c == model.Country }).ToList();
                model.CityOptions = _locationService.GetCities(model.Country).Select(c => new SelectListItem { Text = c, Value = c, Selected = c == model.City }).ToList();
                return View(model);
            }

            var token = Request.Form["g-recaptcha-response"].ToString();
            var ok = await _recaptchaService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), "register");
            if (!ok)
            {
                _logger.LogWarning("Register reCAPTCHA failed for {Email} from {IP}", model.Email, HttpContext.Connection.RemoteIpAddress?.ToString());
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed.");
                model.CountryOptions = _locationService.GetCountries().Select(c => new SelectListItem { Text = c, Value = c, Selected = c == model.Country }).ToList();
                model.CityOptions = _locationService.GetCities(model.Country).Select(c => new SelectListItem { Text = c, Value = c, Selected = c == model.City }).ToList();
                return View(model);
            }

            // Map country/city to currency and timezone
            var currency = _locationService.ResolveCurrency(model.Country) ?? "NZD";
            var timeZoneId = _locationService.ResolveTimeZone(model.Country, model.City) ?? "Pacific/Auckland";

            var generatedUserName = $"user_{Guid.NewGuid().ToString("N").Substring(0, 12)}";
            var user = new ApplicationUser
            {
                UserName = generatedUserName,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Country = model.Country,
                City = model.City,
                PreferredCurrency = currency,
                TimeZoneId = timeZoneId
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                _logger.LogInformation("New user registered {UserId} {Email}", user.Id, user.Email);
                await _userManager.AddToRoleAsync(user, "SiteUser");

                await _locationProvider.SetFromCountryCityAsync(model.Country, model.City, "register");

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account",
                    new { userId, code, returnUrl }, protocol: Request.Scheme);

                try
                {
                    await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                        $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");
                    _logger.LogInformation("Sent confirmation email to {Email}", model.Email);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send confirmation email to {Email}", model.Email);
                    // Continue to confirmation page; user can request resend from login
                }

                return RedirectToAction(nameof(RegisterConfirmation), new { email = model.Email, returnUrl });
            }
            foreach (var error in result.Errors)
            {
                _logger.LogWarning("Registration error for {Email}: {Code} {Desc}", model.Email, error.Code, error.Description);
                ModelState.AddModelError(string.Empty, error.Description);
            }
            model.CountryOptions = _locationService.GetCountries().Select(c => new SelectListItem { Text = c, Value = c, Selected = c == model.Country }).ToList();
            model.CityOptions = _locationService.GetCities(model.Country).Select(c => new SelectListItem { Text = c, Value = c, Selected = c == model.City }).ToList();
            return View(model);
        }

        [HttpGet]
        public IActionResult RegisterConfirmation(string email, string? returnUrl = null)
        {
            ViewBag.Email = email;
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string code, string? returnUrl = null)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, decoded);
            if (!result.Succeeded) return BadRequest();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendEmailConfirmation(string email)
        {
            _logger.LogInformation("Resend email confirmation requested for {Email}", email);
            if (string.IsNullOrWhiteSpace(email))
            {
                return Json(new { ok = false });
            }
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                // Do not reveal existence
                return Json(new { ok = true });
            }
            if (await _userManager.IsEmailConfirmedAsync(user))
            {
                return Json(new { ok = true, alreadyConfirmed = true });
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account",
                new { userId = user.Id, code }, protocol: Request.Scheme);

            try
            {
                await _emailSender.SendEmailAsync(user.Email!, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");
                _logger.LogInformation("Resent confirmation email to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resend confirmation email to {Email}", email);
                Response.StatusCode = 500;
                return Json(new { ok = false });
            }

            return Json(new { ok = true });
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            if (!ModelState.IsValid) return View(model);

            var token = Request.Form["g-recaptcha-response"].ToString();
            var ok = await _recaptchaService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString(), "forgot_password");
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed.");
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null || !(await _userManager.IsEmailConfirmedAsync(user)))
            {
                return RedirectToAction(nameof(ForgotPasswordConfirmation));
            }

            var code = await _userManager.GeneratePasswordResetTokenAsync(user);
            code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
            var callbackUrl = Url.Action(nameof(ResetPassword), "Account",
                new { code }, protocol: Request.Scheme);

            await _emailSender.SendEmailAsync(model.Email, "Reset Password",
                $"Please reset your password by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");

            return RedirectToAction(nameof(ForgotPasswordConfirmation));
        }

        [HttpGet]
        public IActionResult ForgotPasswordConfirmation() => View();

        [HttpGet]
        public IActionResult ResetPassword(string code = "") => View(new ResetPasswordViewModel { Code = code });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null) return RedirectToAction(nameof(ResetPasswordConfirmation));

            var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(model.Code));
            var result = await _userManager.ResetPasswordAsync(user, decoded, model.Password);
            if (result.Succeeded) return RedirectToAction(nameof(ResetPasswordConfirmation));

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetPasswordConfirmation() => View();

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Lockout() => View();

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/account")]
        public async Task<IActionResult> Dashboard()
        {
            return await RenderAccountDashboardAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Manage()
        {
            return await RenderAccountDashboardAsync();
        }

        [HttpGet("/account/payment")]
        public async Task<IActionResult> Payment(string? returnUrl = null)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            if (!CanManageStripeForEvents())
            {
                return Forbid();
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            var usesPlatformStripeAccount = ShouldUsePlatformStripeAccount();
            var model = new AccountPaymentViewModel
            {
                IsAdmin = User.IsInRole("Admin"),
                IsEventManager = User.IsInRole("EventManager"),
                UsesPlatformStripeAccount = usesPlatformStripeAccount,
                StripeEnvironmentLabel = GetStripeEnvironmentLabel(),
                ReturnUrl = returnUrl,
                PlatformFeePercent = _stripeOptions.PlatformFeePercent,
                StripeStatus = await BuildStripeStatusViewModelAsync(user, usesPlatformStripeAccount)
            };

            return View(model);
        }

        [HttpPost("/account/payment/connect")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConnectStripe(string? returnUrl = null)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            if (!CanManageStripeForEvents())
            {
                return Forbid();
            }

            if (ShouldUsePlatformStripeAccount())
            {
                TempData["StripeWarning"] = "Admin accounts use the platform Stripe account. No separate Connect onboarding is required.";
                return RedirectToAction(nameof(Payment), new { returnUrl });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            try
            {
                var connectReturnUrl = Url.Action(nameof(StripeConnectReturn), "Account", new { returnUrl }, Request.Scheme);
                var connectRefreshUrl = Url.Action(nameof(StripeConnectRefresh), "Account", new { returnUrl }, Request.Scheme);

                if (string.IsNullOrWhiteSpace(connectReturnUrl) || string.IsNullOrWhiteSpace(connectRefreshUrl))
                {
                    TempData["StripeError"] = "Unable to create Stripe onboarding link. Please try again.";
                    return RedirectToAction(nameof(Payment), new { returnUrl });
                }

                var onboardingUrl = await _stripeConnectService.CreateOrGetOnboardingLinkAsync(
                    user,
                    connectReturnUrl,
                    connectRefreshUrl,
                    HttpContext.RequestAborted);

                return Redirect(onboardingUrl);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stripe onboarding link creation failed for user {UserId}", user.Id);
                TempData["StripeError"] = "Unable to connect with Stripe right now. Please verify your keys and try again.";
                return RedirectToAction(nameof(Payment), new { returnUrl });
            }
        }

        [HttpGet("/account/payment/return")]
        public async Task<IActionResult> StripeConnectReturn(string? returnUrl = null)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            if (!CanManageStripeForEvents())
            {
                return Forbid();
            }

            if (ShouldUsePlatformStripeAccount())
            {
                TempData["StripeWarning"] = "Admin accounts use the platform Stripe account. Verify platform Stripe setup in Payment Settings.";
                return RedirectToAction(nameof(Payment), new { returnUrl });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            var status = await _stripeConnectService.GetStatusAsync(user, HttpContext.RequestAborted);
            if (status.IsSetupComplete)
            {
                TempData["StripeSuccess"] = "Stripe setup is complete. You can now create and sell events.";
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }
            }
            else
            {
                TempData["StripeWarning"] = "Stripe account linked. Complete all required Stripe steps to enable payouts.";
            }

            return RedirectToAction(nameof(Payment), new { returnUrl });
        }

        [HttpGet("/account/payment/refresh")]
        public IActionResult StripeConnectRefresh(string? returnUrl = null)
        {
            if (ShouldUsePlatformStripeAccount())
            {
                TempData["StripeWarning"] = "Admin accounts use the platform Stripe account. No individual Connect onboarding step is required.";
                return RedirectToAction(nameof(Payment), new { returnUrl });
            }

            TempData["StripeWarning"] = "Stripe setup was not completed yet. Continue onboarding to finish.";
            return RedirectToAction(nameof(Payment), new { returnUrl });
        }

        [HttpPost("/account/payment/dashboard")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OpenStripeDashboard(string? returnUrl = null)
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            if (!CanManageStripeForEvents())
            {
                return Forbid();
            }

            if (ShouldUsePlatformStripeAccount())
            {
                return Redirect(GetPlatformDashboardUrl());
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login), new { returnUrl = "/account/payment" });
            }

            var loginLink = await _stripeConnectService.CreateDashboardLoginLinkAsync(user, HttpContext.RequestAborted);
            if (string.IsNullOrWhiteSpace(loginLink))
            {
                TempData["StripeError"] = "Unable to open Stripe dashboard. Make sure your account is fully connected.";
                return RedirectToAction(nameof(Payment), new { returnUrl });
            }

            return Redirect(loginLink);
        }

        private bool CanManageStripeForEvents()
        {
            return User.IsInRole("Admin") || User.IsInRole("EventManager");
        }

        private bool ShouldUsePlatformStripeAccount()
        {
            return User.IsInRole("Admin");
        }

        private string GetStripeEnvironmentLabel()
        {
            if (string.IsNullOrWhiteSpace(_stripeOptions.SecretKey))
            {
                return "Not Configured";
            }

            if (_stripeOptions.SecretKey.StartsWith("sk_live_"))
            {
                return "Live";
            }

            if (_stripeOptions.SecretKey.StartsWith("sk_test_"))
            {
                return "Test";
            }

            return "Unknown";
        }

        private string GetPlatformDashboardUrl()
        {
            return GetStripeEnvironmentLabel() == "Live"
                ? "https://dashboard.stripe.com/"
                : "https://dashboard.stripe.com/test/";
        }

        private async Task<IActionResult> RenderAccountDashboardAsync()
        {
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction(nameof(Login));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var model = new AccountManageViewModel
            {
                IsAdmin = User.IsInRole("Admin"),
                IsEventManager = User.IsInRole("EventManager")
            };

            if (model.CanManageEvents)
            {
                model.HasAnyEvents = await _db.Events
                    .AsNoTracking()
                    .AnyAsync(e => e.CreatedByUserId == user.Id, HttpContext.RequestAborted);
                model.StripeStatus = await BuildStripeStatusViewModelAsync(user, ShouldUsePlatformStripeAccount());
            }

            return View("Manage", model);
        }

        private async Task<StripeConnectionStatusViewModel> BuildStripeStatusViewModelAsync(
            ApplicationUser user,
            bool usePlatformStripeAccount)
        {
            var status = usePlatformStripeAccount
                ? await _stripeConnectService.GetPlatformStatusAsync(HttpContext.RequestAborted)
                : await _stripeConnectService.GetStatusAsync(user, HttpContext.RequestAborted);
            return new StripeConnectionStatusViewModel
            {
                IsStripeConfigured = status.IsStripeConfigured,
                StripeAccountId = status.StripeAccountId,
                StripeAccountType = status.StripeAccountType,
                ChargesEnabled = status.ChargesEnabled,
                PayoutsEnabled = status.PayoutsEnabled,
                ErrorMessage = status.ErrorMessage,
                RequirementsDisabledReason = status.RequirementsDisabledReason,
                CapabilityIssues = status.CapabilityIssues,
                MissingRequirements = status.MissingRequirements
            };
        }

        // mapping logic moved to ILocationService

        public class LoginViewModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
            [Required, DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public class RegisterViewModel
        {
            [Required, StringLength(100), Display(Name = "First name")]
            public string FirstName { get; set; } = string.Empty;

            [Required, StringLength(100), Display(Name = "Last name")]
            public string LastName { get; set; } = string.Empty;

            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(100, MinimumLength = 10), DataType(DataType.Password)]
            [RegularExpression("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)(?=.*[^\\w\\s]).{10,}$",
                ErrorMessage = "Password must be at least 10 chars and include upper, lower, digit, and symbol.")]
            public string Password { get; set; } = string.Empty;
            [DataType(DataType.Password), Display(Name = "Confirm password"), Compare("Password")]
            public string ConfirmPassword { get; set; } = string.Empty;
            [Required]
            [Display(Name = "Country")]
            public string Country { get; set; } = "New Zealand";
            [Required]
            [Display(Name = "City")]
            public string City { get; set; } = "Auckland";
            public IEnumerable<SelectListItem> CountryOptions { get; set; } = Enumerable.Empty<SelectListItem>();
            public IEnumerable<SelectListItem> CityOptions { get; set; } = Enumerable.Empty<SelectListItem>();
            // Computed server-side:
            public string? PreferredCurrency { get; set; }
            public string? TimeZoneId { get; set; }
        }

        public class ForgotPasswordViewModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
        }

        public class ResetPasswordViewModel
        {
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
            [DataType(DataType.Password), Display(Name = "Confirm password"), Compare("Password")]
            public string ConfirmPassword { get; set; } = string.Empty;
            public string Code { get; set; } = string.Empty;
        }
    }
}


