using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RailTix.Data;
using RailTix.Models.Domain;
using RailTix.Models.ViewModels.Cms;

namespace RailTix.Services.Cms
{
    public class CmsPageRenderer
    {
        private const int DefaultEventListLimit = 10;
        private const int MaxEventListLimit = 50;

        private readonly ApplicationDbContext _db;
        private readonly ICmsUrlService _urlService;

        public CmsPageRenderer(ApplicationDbContext db, ICmsUrlService urlService)
        {
            _db = db;
            _urlService = urlService;
        }

        public async Task<CmsPageViewModel?> GetPageByPathAsync(string path)
        {
            var normalized = _urlService.NormalizePath(path);
            var page = await _db.CmsPages
                .AsNoTracking()
                .Include(p => p.Components)
                .FirstOrDefaultAsync(p => p.Path == normalized && p.IsPublished);

            return page == null ? null : await MapToViewModelAsync(page);
        }

        public async Task<CmsPageViewModel?> GetHomepageAsync()
        {
            var page = await _db.CmsPages
                .AsNoTracking()
                .Include(p => p.Components)
                .FirstOrDefaultAsync(p => p.IsHomepage && p.IsPublished);

            return page == null ? null : await MapToViewModelAsync(page);
        }

        private async Task<CmsPageViewModel> MapToViewModelAsync(CmsPage page)
        {
            var components = new List<CmsComponentViewModel>();
            foreach (var component in page.Components.Where(c => c.IsEnabled).OrderBy(c => c.Position))
            {
                components.Add(await MapComponentAsync(component));
            }

            return new CmsPageViewModel
            {
                Title = page.Title,
                Path = page.Path,
                IsHomepage = page.IsHomepage,
                Components = components
            };
        }

        private async Task<CmsComponentViewModel> MapComponentAsync(CmsPageComponent component)
        {
            var viewModel = new CmsComponentViewModel
            {
                Type = component.Type
            };

            if (component.Type == CmsComponentTypes.Image)
            {
                viewModel.Image = Deserialize<CmsImageSettings>(component.SettingsJson);
            }
            else if (component.Type == CmsComponentTypes.Banner)
            {
                viewModel.Banner = Deserialize<CmsBannerSettings>(component.SettingsJson);
            }
            else if (component.Type == CmsComponentTypes.EventList)
            {
                var settings = Deserialize<CmsEventListSettings>(component.SettingsJson) ?? new CmsEventListSettings();
                viewModel.EventList = await BuildEventListAsync(settings);
            }

            return viewModel;
        }

        private async Task<CmsEventListComponentViewModel> BuildEventListAsync(CmsEventListSettings settings)
        {
            var source = NormalizeSource(settings.Source);
            var limit = NormalizeLimit(settings.Limit);

            var response = new CmsEventListComponentViewModel
            {
                Source = source,
                Category = settings.Category?.Trim(),
                Location = settings.Location?.Trim(),
                Limit = limit
            };

            var baseQuery = _db.Events
                .AsNoTracking()
                .Where(e => e.Status == EventStatuses.Live);

            if (source == "manual")
            {
                var manualIds = ParseEventIds(settings.EventIds);
                if (manualIds.Count == 0)
                {
                    return response;
                }

                var events = await baseQuery
                    .Where(e => manualIds.Contains(e.Id))
                    .ToListAsync();

                var lookup = events.ToDictionary(e => e.Id, e => e);
                foreach (var id in manualIds)
                {
                    if (!lookup.TryGetValue(id, out var item))
                    {
                        continue;
                    }

                    response.Events.Add(MapEvent(item));
                    if (response.Events.Count >= limit)
                    {
                        break;
                    }
                }

                return response;
            }

            // Query mode supports simple location/category filtering in v1.
            if (!string.IsNullOrWhiteSpace(response.Location))
            {
                var location = response.Location!;
                baseQuery = baseQuery.Where(e =>
                    (e.City != null && EF.Functions.Like(e.City, $"%{location}%")) ||
                    (e.Region != null && EF.Functions.Like(e.Region, $"%{location}%")) ||
                    (e.Country != null && EF.Functions.Like(e.Country, $"%{location}%")) ||
                    (e.VenueName != null && EF.Functions.Like(e.VenueName, $"%{location}%")));
            }

            if (!string.IsNullOrWhiteSpace(response.Category))
            {
                // Category support is deferred on the Event model; keep the setting for forward compatibility.
            }

            var queryEvents = await baseQuery
                .OrderBy(e => e.StartsAtLocal)
                .ThenBy(e => e.Title)
                .Take(limit)
                .ToListAsync();

            response.Events = queryEvents.Select(MapEvent).ToList();
            return response;
        }

        private static string NormalizeSource(string? source)
        {
            return string.Equals(source?.Trim(), "manual", System.StringComparison.OrdinalIgnoreCase)
                ? "manual"
                : "query";
        }

        private static int NormalizeLimit(int? limit)
        {
            if (!limit.HasValue || limit.Value <= 0)
            {
                return DefaultEventListLimit;
            }

            return limit.Value > MaxEventListLimit ? MaxEventListLimit : limit.Value;
        }

        private static List<System.Guid> ParseEventIds(string? eventIds)
        {
            if (string.IsNullOrWhiteSpace(eventIds))
            {
                return new List<System.Guid>();
            }

            var parsed = new List<System.Guid>();
            foreach (var token in eventIds.Split(',', System.StringSplitOptions.RemoveEmptyEntries))
            {
                if (System.Guid.TryParse(token.Trim(), out var id) && !parsed.Contains(id))
                {
                    parsed.Add(id);
                }
            }

            return parsed;
        }

        private static CmsEventListItemViewModel MapEvent(Event entity)
        {
            return new CmsEventListItemViewModel
            {
                Id = entity.Id,
                Title = entity.Title,
                Slug = entity.Slug,
                StartsAtLocal = entity.StartsAtLocal,
                EndsAtLocal = entity.EndsAtLocal,
                VenueName = entity.VenueName,
                City = entity.City,
                Region = entity.Region,
                Country = entity.Country
            };
        }

        private static T? Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json);
        }
    }
}

