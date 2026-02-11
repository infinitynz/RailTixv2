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

            return page == null ? null : MapToViewModel(page);
        }

        public async Task<CmsPageViewModel?> GetHomepageAsync()
        {
            var page = await _db.CmsPages
                .AsNoTracking()
                .Include(p => p.Components)
                .FirstOrDefaultAsync(p => p.IsHomepage && p.IsPublished);

            return page == null ? null : MapToViewModel(page);
        }

        private CmsPageViewModel MapToViewModel(CmsPage page)
        {
            var components = page.Components
                .Where(c => c.IsEnabled)
                .OrderBy(c => c.Position)
                .Select(MapComponent)
                .ToList();

            return new CmsPageViewModel
            {
                Title = page.Title,
                Path = page.Path,
                IsHomepage = page.IsHomepage,
                Components = components
            };
        }

        private CmsComponentViewModel MapComponent(CmsPageComponent component)
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
                viewModel.EventList = Deserialize<CmsEventListSettings>(component.SettingsJson);
            }

            return viewModel;
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

