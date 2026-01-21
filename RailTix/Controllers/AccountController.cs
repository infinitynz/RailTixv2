using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using RailTix.Models.Domain;
using RailTix.Models.Options;
using RailTix.Services.Recaptcha;
using System.ComponentModel.DataAnnotations;

namespace RailTix.Controllers
{
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly IGoogleRecaptchaService _recaptchaService;
        private readonly GoogleRecaptchaOptions _recaptchaOptions;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            IGoogleRecaptchaService recaptchaService,
            IOptions<GoogleRecaptchaOptions> recaptchaOptions)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _recaptchaService = recaptchaService;
            _recaptchaOptions = recaptchaOptions.Value;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
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
            var ok = await _recaptchaService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed.");
                return View(model);
            }

            var result = await _signInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                return RedirectToLocal(returnUrl);
            }
            if (result.RequiresTwoFactor)
            {
                // future 2FA flow
                ModelState.AddModelError(string.Empty, "Two-factor authentication is required.");
                return View(model);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return View(model);
        }

        [HttpGet]
        public IActionResult Register(string? returnUrl = null)
        {
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            ViewBag.ReturnUrl = returnUrl;
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, string? returnUrl = null)
        {
            ViewBag.SiteKey = _recaptchaOptions.SiteKey;
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var token = Request.Form["g-recaptcha-response"].ToString();
            var ok = await _recaptchaService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString());
            if (!ok)
            {
                ModelState.AddModelError(string.Empty, "reCAPTCHA validation failed.");
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                PreferredCurrency = model.PreferredCurrency ?? "NZD",
                TimeZoneId = model.TimeZoneId ?? "Pacific/Auckland"
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, "SiteUser");

                var userId = await _userManager.GetUserIdAsync(user);
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                code = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(code));
                var callbackUrl = Url.Action(nameof(ConfirmEmail), "Account",
                    new { userId, code, returnUrl }, protocol: Request.Scheme);

                await _emailSender.SendEmailAsync(model.Email, "Confirm your email",
                    $"Please confirm your account by <a href='{HtmlEncoder.Default.Encode(callbackUrl!)}'>clicking here</a>.");

                return RedirectToAction(nameof(RegisterConfirmation), new { email = model.Email, returnUrl });
            }
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
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
            var ok = await _recaptchaService.VerifyAsync(token, HttpContext.Connection.RemoteIpAddress?.ToString());
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
            [Required, EmailAddress]
            public string Email { get; set; } = string.Empty;
            [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;
            [DataType(DataType.Password), Display(Name = "Confirm password"), Compare("Password")]
            public string ConfirmPassword { get; set; } = string.Empty;
            [Display(Name = "Preferred currency (ISO)")]
            public string? PreferredCurrency { get; set; } = "NZD";
            [Display(Name = "Time zone (IANA)")]
            public string? TimeZoneId { get; set; } = "Pacific/Auckland";
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


