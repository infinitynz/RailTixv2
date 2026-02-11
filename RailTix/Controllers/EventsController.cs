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
        private readonly ILogger<EventsController> _logger;

        public EventsController(
            ApplicationDbContext db,
            UserManager<ApplicationUser> userManager,
            ICmsUrlService urlService,
            ILocationService locationService,
            ILogger<EventsController> logger)
        {
            _db = db;
            _userManager = userManager;
            _urlService = urlService;
            _locationService = locationService;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var userId = _userManager.GetUserId(User);
            var isAdmin = User.IsInRole("Admin");
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

            ValidateDates(model);
            var baseSlug = _urlService.NormalizeSegment(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                ModelState.AddModelError(nameof(model.Slug), "Please provide a valid slug.");
            }

            var slug = await EnsureUniqueSlugAsync(baseSlug);

            if (!ModelState.IsValid)
            {
                model.Slug = baseSlug;
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

        private void ValidateDates(EventEditViewModel model)
        {
            if (!model.StartsAtLocal.HasValue || !model.EndsAtLocal.HasValue)
            {
                return;
            }

            if (model.EndsAtLocal.Value <= model.StartsAtLocal.Value)
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
    }
}

