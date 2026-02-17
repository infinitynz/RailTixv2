using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RailTix.Data;
using RailTix.Models.Domain;
using RailTix.Models.ViewModels.Events;
using RailTix.Services.Cms;
using RailTix.Services.Location;
using RailTix.Services.Payments;

namespace RailTix.Controllers
{
    [Authorize(Roles = "Admin,EventManager")]
    [Route("account/events")]
    public class EventsController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICmsUrlService _urlService;
        private readonly ILocationService _locationService;
        private readonly IStripeConnectService _stripeConnectService;
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ICmsUrlService urlService,
            ILocationService locationService,
            IStripeConnectService stripeConnectService,
            ILogger<EventsController> logger)
        {
            _db = db;
            _userManager = userManager;
            _urlService = urlService;
            _locationService = locationService;
            _stripeConnectService = stripeConnectService;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
            var currentUser = await _userManager.GetUserAsync(User);
            var canCreateEvents = false;
            string? createEventsBlockedReason = null;

            if (isAdmin)
            {
                var platformStatus = await _stripeConnectService.GetPlatformStatusAsync(HttpContext.RequestAborted);
                canCreateEvents = platformStatus.IsSetupComplete;
                createEventsBlockedReason = canCreateEvents
                    ? null
                    : (string.IsNullOrWhiteSpace(platformStatus.ErrorMessage)
                        ? "Platform Stripe account is not ready. Verify Stripe configuration in Payment Settings."
                        : platformStatus.ErrorMessage);
            }
            else
            {
                var stripeStatus = currentUser == null
                    ? null
                    : await _stripeConnectService.GetStatusAsync(currentUser, HttpContext.RequestAborted);
                canCreateEvents = stripeStatus?.IsSetupComplete ?? false;
                createEventsBlockedReason = canCreateEvents
                    ? null
                    : (string.IsNullOrWhiteSpace(stripeStatus?.ErrorMessage)
                        ? "Connect Stripe in Payment Settings before creating events."
                        : stripeStatus?.ErrorMessage);
            }
            var query = _db.Events.AsNoTracking();
            if (!isAdmin && !string.IsNullOrWhiteSpace(userId))
            {
                query = query.Where(e => e.CreatedByUserId == userId);
            }

            var events = await query
                .OrderByDescending(e => e.CreatedAt)
                .ToListAsync();

            var model = new EventListViewModel
            {
                IsAdmin = isAdmin,
                CanCreateEvents = canCreateEvents,
                CreateEventsBlockedReason = createEventsBlockedReason,
                Events = events.Select(e => new EventListItemViewModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    Slug = e.Slug,
                    Status = e.Status,
                    StartsAtLocal = e.StartsAtLocal,
                    EndsAtLocal = e.EndsAtLocal,
                    TimeZoneId = e.TimeZoneId,
                    CreatedAt = e.CreatedAt
                }).ToList()
            };

            return View(model);
        }

        [HttpGet("create")]
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            var stripeGuardResult = await EnsureStripeReadyForEventCreationAsync(user);
            if (stripeGuardResult != null)
            {
                return stripeGuardResult;
            }

            var now = DateTime.Now;
            var defaultStart = now.AddDays(7).Date.AddHours(19);
            var defaultEnd = defaultStart.AddHours(3);

            var timeZoneId = user?.TimeZoneId;
            if (string.IsNullOrWhiteSpace(timeZoneId) && !string.IsNullOrWhiteSpace(user?.Country))
            {
                timeZoneId = _locationService.ResolveTimeZone(user.Country!, user.City ?? string.Empty);
            }
            timeZoneId = string.IsNullOrWhiteSpace(timeZoneId) ? "Pacific/Auckland" : timeZoneId;

            var currency = user?.PreferredCurrency;
            if (string.IsNullOrWhiteSpace(currency) && !string.IsNullOrWhiteSpace(user?.Country))
            {
                currency = _locationService.ResolveCurrency(user.Country!);
            }
            currency = string.IsNullOrWhiteSpace(currency) ? "NZD" : currency;

            var model = new EventEditViewModel
            {
                StartsAtLocal = defaultStart,
                EndsAtLocal = defaultEnd,
                TimeZoneId = timeZoneId,
                CurrencyCode = currency,
                OrganizerName = GetUserDisplayName(user),
                Country = user?.Country,
                City = user?.City
            };

            return View(model);
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EventEditViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var stripeGuardResult = await EnsureStripeReadyForEventCreationAsync(user);
            if (stripeGuardResult != null)
            {
                return stripeGuardResult;
            }

            ValidateDates(model);
            var baseSlug = _urlService.NormalizeSegment(model.Title);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                ModelState.AddModelError(nameof(model.Title), "Please provide a valid event title.");
            }

            var slug = await EnsureUniqueSlugAsync(baseSlug);

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var now = DateTime.UtcNow;
            var organizerName = string.IsNullOrWhiteSpace(model.OrganizerName)
                ? GetUserDisplayName(user)
                : model.OrganizerName.Trim();

            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                Title = model.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(model.Description) ? null : model.Description.Trim(),
                Slug = slug,
                Status = EventStatuses.Draft,
                StartsAtLocal = DateTime.SpecifyKind(model.StartsAtLocal!.Value, DateTimeKind.Unspecified),
                EndsAtLocal = DateTime.SpecifyKind(model.EndsAtLocal!.Value, DateTimeKind.Unspecified),
                TimeZoneId = model.TimeZoneId.Trim(),
                CurrencyCode = model.CurrencyCode.Trim().ToUpperInvariant(),
                OrganizerName = string.IsNullOrWhiteSpace(organizerName) ? null : organizerName,
                VenueName = string.IsNullOrWhiteSpace(model.VenueName) ? null : model.VenueName.Trim(),
                AddressLine1 = string.IsNullOrWhiteSpace(model.AddressLine1) ? null : model.AddressLine1.Trim(),
                AddressLine2 = string.IsNullOrWhiteSpace(model.AddressLine2) ? null : model.AddressLine2.Trim(),
                City = string.IsNullOrWhiteSpace(model.City) ? null : model.City.Trim(),
                Region = string.IsNullOrWhiteSpace(model.Region) ? null : model.Region.Trim(),
                Country = string.IsNullOrWhiteSpace(model.Country) ? null : model.Country.Trim(),
                PostalCode = string.IsNullOrWhiteSpace(model.PostalCode) ? null : model.PostalCode.Trim(),
                CreatedByUserId = user.Id,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.Events.Add(newEvent);
            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Event create failed for {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Unable to save event. Please try again.");
                return View(model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost("slug/validate")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ValidateSlug(EventSlugRequest request)
        {
            if (request == null || request.EventId == Guid.Empty)
            {
                return BadRequest(new { ok = false, message = "Invalid event reference." });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { ok = false, message = "You must be signed in." });
            }

            var targetEvent = await FindEditableEventAsync(
                request.EventId,
                userId,
                User.IsInRole("Admin"),
                HttpContext.RequestAborted);

            if (targetEvent == null)
            {
                return NotFound(new { ok = false, message = "Event not found." });
            }

            var validation = await ValidateEventSlugAsync(targetEvent.Id, request.Slug, HttpContext.RequestAborted);
            return Json(new
            {
                ok = validation.IsValid,
                isAvailable = validation.IsAvailable,
                normalizedSlug = validation.NormalizedSlug,
                message = validation.Message
            });
        }

        [HttpPost("slug/update")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateSlug(EventSlugRequest request)
        {
            if (request == null || request.EventId == Guid.Empty)
            {
                return BadRequest(new { ok = false, message = "Invalid event reference." });
            }

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized(new { ok = false, message = "You must be signed in." });
            }

            var targetEvent = await FindEditableEventAsync(
                request.EventId,
                userId,
                User.IsInRole("Admin"),
                HttpContext.RequestAborted);

            if (targetEvent == null)
            {
                return NotFound(new { ok = false, message = "Event not found." });
            }

            var validation = await ValidateEventSlugAsync(targetEvent.Id, request.Slug, HttpContext.RequestAborted);
            if (!validation.IsValid)
            {
                return BadRequest(new
                {
                    ok = false,
                    isAvailable = validation.IsAvailable,
                    normalizedSlug = validation.NormalizedSlug,
                    message = validation.Message
                });
            }

            targetEvent.Slug = validation.NormalizedSlug;
            targetEvent.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Slug update failed for event {EventId}", targetEvent.Id);
                return Conflict(new { ok = false, message = "Unable to update URL right now. Please try again." });
            }

            return Json(new
            {
                ok = true,
                slug = targetEvent.Slug,
                message = "Event URL updated."
            });
        }

        private async Task<IActionResult?> EnsureStripeReadyForEventCreationAsync(ApplicationUser? user)
        {
            if (user == null)
            {
                return Unauthorized();
            }

            if (User.IsInRole("Admin"))
            {
                var platformStatus = await _stripeConnectService.GetPlatformStatusAsync(HttpContext.RequestAborted);
                if (platformStatus.IsSetupComplete)
                {
                    return null;
                }

                TempData["StripeError"] = string.IsNullOrWhiteSpace(platformStatus.ErrorMessage)
                    ? "Platform Stripe account is not ready. Verify Stripe configuration in Payment Settings before creating events."
                    : platformStatus.ErrorMessage;

                var adminReturnUrl = Url.Action(nameof(Create), "Events");
                return RedirectToAction("Payment", "Account", new { returnUrl = adminReturnUrl });
            }

            var stripeStatus = await _stripeConnectService.GetStatusAsync(user, HttpContext.RequestAborted);
            if (stripeStatus.IsSetupComplete)
            {
                return null;
            }

            TempData["StripeError"] = string.IsNullOrWhiteSpace(stripeStatus.ErrorMessage)
                ? "Connect Stripe and complete onboarding before creating events."
                : stripeStatus.ErrorMessage;

            var returnUrl = Url.Action(nameof(Create), "Events");
            return RedirectToAction("Payment", "Account", new { returnUrl });
        }

        private async Task<Event?> FindEditableEventAsync(
            Guid eventId,
            string userId,
            bool isAdmin,
            System.Threading.CancellationToken cancellationToken)
        {
            var query = _db.Events.Where(e => e.Id == eventId);
            if (!isAdmin)
            {
                query = query.Where(e => e.CreatedByUserId == userId);
            }

            return await query.FirstOrDefaultAsync(cancellationToken);
        }

        private async Task<SlugValidationResult> ValidateEventSlugAsync(
            Guid eventId,
            string? requestedSlug,
            System.Threading.CancellationToken cancellationToken)
        {
            const int maxSlugLength = 200;
            var raw = (requestedSlug ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(raw))
            {
                return SlugValidationResult.Invalid("Enter a URL slug to continue.");
            }

            if (raw.Length > maxSlugLength)
            {
                return SlugValidationResult.Invalid("URL slug is too long.");
            }

            var normalized = _urlService.NormalizeSegment(raw);
            if (normalized.Length > maxSlugLength)
            {
                normalized = normalized.Substring(0, maxSlugLength).Trim('-');
            }

            if (string.IsNullOrWhiteSpace(normalized))
            {
                return SlugValidationResult.Invalid("URL slug contains unsupported characters.");
            }

            var isTaken = await _db.Events
                .AsNoTracking()
                .AnyAsync(e => e.Id != eventId && e.Slug == normalized, cancellationToken);

            if (isTaken)
            {
                return SlugValidationResult.Invalid("That URL is already taken.");
            }

            var changed = !string.Equals(raw, normalized, StringComparison.Ordinal);
            var message = changed
                ? $"URL is available. It will be saved as '{normalized}'."
                : "URL is available.";
            return SlugValidationResult.Available(normalized, message);
        }

        private void ValidateDates(EventEditViewModel model)
        {
            if (!model.StartsAtLocal.HasValue || !model.EndsAtLocal.HasValue)
            {
                return;
            }

            var now = DateTime.Now;
            var startsAt = model.StartsAtLocal.Value;
            var endsAt = model.EndsAtLocal.Value;

            if (startsAt < now)
            {
                ModelState.AddModelError(nameof(model.StartsAtLocal), "Start date/time must be in the future.");
            }

            if (endsAt < now)
            {
                ModelState.AddModelError(nameof(model.EndsAtLocal), "End date/time must be in the future.");
            }

            if (endsAt <= startsAt)
            {
                ModelState.AddModelError(nameof(model.EndsAtLocal), "End date/time must be after the start date/time.");
            }
        }

        private async Task<string> EnsureUniqueSlugAsync(string baseSlug)
        {
            var slug = string.IsNullOrWhiteSpace(baseSlug) ? "event" : baseSlug;
            var existing = await _db.Events
                .AsNoTracking()
                .Select(e => e.Slug)
                .ToListAsync();

            var suffix = 2;
            while (existing.Any(s => string.Equals(s, slug, StringComparison.OrdinalIgnoreCase)))
            {
                slug = $"{baseSlug}-{suffix++}";
            }

            return slug;
        }

        private static string? GetUserDisplayName(ApplicationUser? user)
        {
            if (user == null)
            {
                return null;
            }

            var name = $"{user.FirstName} {user.LastName}".Trim();
            return string.IsNullOrWhiteSpace(name) ? user.Email : name;
        }

        public sealed class EventSlugRequest
        {
            public Guid EventId { get; set; }
            public string? Slug { get; set; }
        }

        private sealed class SlugValidationResult
        {
            private SlugValidationResult(bool isValid, bool isAvailable, string normalizedSlug, string message)
            {
                IsValid = isValid;
                IsAvailable = isAvailable;
                NormalizedSlug = normalizedSlug;
                Message = message;
            }

            public bool IsValid { get; }
            public bool IsAvailable { get; }
            public string NormalizedSlug { get; }
            public string Message { get; }

            public static SlugValidationResult Available(string normalizedSlug, string message)
                => new SlugValidationResult(true, true, normalizedSlug, message);

            public static SlugValidationResult Invalid(string message)
                => new SlugValidationResult(false, false, string.Empty, message);
        }
    }
}

