using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RailTix.Data;
using RailTix.Models.Domain;
using RailTix.Models.ViewModels.Cms.Admin;
using RailTix.Services.Cms;

namespace RailTix.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("account/admin/content")]
    public class AdminContentController : Controller
    {
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly ApplicationDbContext _db;
        private readonly ICmsUrlService _urlService;
        private readonly ICmsReservedRouteService _reservedRouteService;
        private readonly ILogger<AdminContentController> _logger;

        public AdminContentController(
            ApplicationDbContext db,
            ICmsUrlService urlService,
            ICmsReservedRouteService reservedRouteService,
            ILogger<AdminContentController> logger)
        {
            _db = db;
            _urlService = urlService;
            _reservedRouteService = reservedRouteService;
            _logger = logger;
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction(nameof(Pages));
        }

        [HttpGet("pages")]
        public async Task<IActionResult> Pages()
        {
            var pages = await _db.CmsPages.AsNoTracking().ToListAsync();
            var tree = BuildTree(pages, null);
            var model = new CmsPageListViewModel { Pages = tree };
            return View(model);
        }

        [HttpGet("pages/create")]
        public async Task<IActionResult> Create(Guid? parentId = null)
        {
            var homepageId = await _db.CmsPages
                .Where(p => p.IsHomepage)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var model = new CmsPageEditViewModel
            {
                ParentId = parentId ?? homepageId,
                IsPublished = true
            };

            await PopulateParentOptionsAsync(model, excludeId: null);
            return View(model);
        }

        [HttpPost("pages/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CmsPageEditViewModel model)
        {
            await PopulateParentOptionsAsync(model, excludeId: null);

            var parent = await ResolveParentAsync(model.ParentId);
            if (parent == null)
            {
                ModelState.AddModelError(nameof(model.ParentId), "Parent page is required.");
            }

            var baseSlug = _urlService.NormalizeSegment(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);
            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "page";
            }

            var slug = parent == null
                ? baseSlug
                : await EnsureUniqueSlugAsync(parent.Id, baseSlug, null);

            var customUrl = NormalizeCustomUrl(model.CustomUrl);
            var path = !string.IsNullOrWhiteSpace(customUrl)
                ? customUrl
                : BuildPath(parent, slug);

            if (!await IsPathAllowedAsync(path))
            {
                ModelState.AddModelError(nameof(model.CustomUrl), "That URL is reserved and cannot be used.");
            }

            if (!await IsPathUniqueAsync(path, null))
            {
                ModelState.AddModelError(nameof(model.CustomUrl), "That URL is already in use.");
            }

            if (!ModelState.IsValid)
            {
                model.Slug = slug;
                model.CurrentPath = path;
                return View(model);
            }

            var now = DateTime.UtcNow;
            var position = model.Position > 0 ? model.Position : await GetNextPositionAsync(parent?.Id);

            var page = new CmsPage
            {
                Id = Guid.NewGuid(),
                Title = model.Title.Trim(),
                Slug = slug,
                Path = path,
                ParentId = parent?.Id,
                Position = position,
                IsHomepage = false,
                IsPublished = model.IsPublished,
                CustomUrl = string.IsNullOrWhiteSpace(customUrl) ? null : customUrl,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.CmsPages.Add(page);
            if (!await TrySaveChangesAsync())
            {
                await PopulateParentOptionsAsync(model, excludeId: null);
                return View(model);
            }

            return RedirectToAction(nameof(Edit), new { id = page.Id });
        }

        [HttpGet("pages/{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var page = await _db.CmsPages
                .Include(p => p.Components)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (page == null)
            {
                return NotFound();
            }

            var model = new CmsPageEditViewModel
            {
                Id = page.Id,
                Title = page.Title,
                Slug = page.Slug,
                CustomUrl = page.CustomUrl,
                ParentId = page.ParentId,
                Position = page.Position,
                IsHomepage = page.IsHomepage,
                IsPublished = page.IsPublished,
                CurrentPath = page.Path,
                Components = page.Components
                    .OrderBy(c => c.Position)
                    .Select(c => new CmsComponentListItemViewModel
                    {
                        Id = c.Id,
                        Type = c.Type,
                        Position = c.Position,
                        IsEnabled = c.IsEnabled
                    })
                    .ToList()
            };

            await PopulateParentOptionsAsync(model, excludeId: page.Id);
            return View(model);
        }

        [HttpPost("pages/{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CmsPageEditViewModel model)
        {
            var page = await _db.CmsPages
                .Include(p => p.Components)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (page == null)
            {
                return NotFound();
            }

            await PopulateParentOptionsAsync(model, excludeId: page.Id);

            if (page.IsHomepage)
            {
                model.ParentId = null;
            }

            if (model.ParentId == page.Id)
            {
                ModelState.AddModelError(nameof(model.ParentId), "A page cannot be its own parent.");
            }

            if (model.ParentId.HasValue && await IsDescendantAsync(page.Id, model.ParentId.Value))
            {
                ModelState.AddModelError(nameof(model.ParentId), "A page cannot be moved under one of its descendants.");
            }

            var parent = page.IsHomepage ? null : await ResolveParentAsync(model.ParentId);
            if (!page.IsHomepage && parent == null)
            {
                ModelState.AddModelError(nameof(model.ParentId), "Parent page is required.");
            }

            var baseSlug = page.IsHomepage
                ? page.Slug
                : _urlService.NormalizeSegment(string.IsNullOrWhiteSpace(model.Slug) ? model.Title : model.Slug);

            if (string.IsNullOrWhiteSpace(baseSlug))
            {
                baseSlug = "page";
            }

            var slug = page.IsHomepage
                ? page.Slug
                : await EnsureUniqueSlugAsync(parent?.Id, baseSlug, page.Id);

            var customUrl = page.IsHomepage ? null : NormalizeCustomUrl(model.CustomUrl);
            var path = page.IsHomepage
                ? "/"
                : (!string.IsNullOrWhiteSpace(customUrl) ? customUrl : BuildPath(parent, slug));

            if (!await IsPathAllowedAsync(path))
            {
                ModelState.AddModelError(nameof(model.CustomUrl), "That URL is reserved and cannot be used.");
            }

            if (!await IsPathUniqueAsync(path, page.Id))
            {
                ModelState.AddModelError(nameof(model.CustomUrl), "That URL is already in use.");
            }

            if (!ModelState.IsValid)
            {
                model.Slug = slug;
                model.CurrentPath = path;
                model.Components = page.Components
                    .OrderBy(c => c.Position)
                    .Select(c => new CmsComponentListItemViewModel
                    {
                        Id = c.Id,
                        Type = c.Type,
                        Position = c.Position,
                        IsEnabled = c.IsEnabled
                    })
                    .ToList();
                return View(model);
            }

            var now = DateTime.UtcNow;
            var originalPath = page.Path;

            page.Title = model.Title.Trim();
            page.Slug = slug;
            page.Path = path;
            page.ParentId = page.IsHomepage ? null : parent?.Id;
            page.Position = model.Position;
            page.IsPublished = model.IsPublished;
            page.CustomUrl = string.IsNullOrWhiteSpace(customUrl) ? null : customUrl;
            page.UpdatedAt = now;

            if (!string.Equals(originalPath, page.Path, StringComparison.Ordinal))
            {
                await UpdateDescendantPathsAsync(page);
            }

            if (!await TrySaveChangesAsync())
            {
                model.CurrentPath = page.Path;
                model.Components = page.Components
                    .OrderBy(c => c.Position)
                    .Select(c => new CmsComponentListItemViewModel
                    {
                        Id = c.Id,
                        Type = c.Type,
                        Position = c.Position,
                        IsEnabled = c.IsEnabled
                    })
                    .ToList();
                return View(model);
            }

            return RedirectToAction(nameof(Edit), new { id = page.Id });
        }

        [HttpPost("pages/{id:guid}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var page = await _db.CmsPages.FirstOrDefaultAsync(p => p.Id == id);
            if (page == null)
            {
                return NotFound();
            }

            if (page.IsHomepage)
            {
                return RedirectToAction(nameof(Edit), new { id = page.Id });
            }

            await DeletePageTreeAsync(page);
            await TrySaveChangesAsync();

            return RedirectToAction(nameof(Pages));
        }

        [HttpGet("pages/{pageId:guid}/components/create")]
        public async Task<IActionResult> CreateComponent(Guid pageId, string? type = null)
        {
            var page = await _db.CmsPages.AsNoTracking().FirstOrDefaultAsync(p => p.Id == pageId);
            if (page == null)
            {
                return NotFound();
            }

            var requestedType = IsSupportedComponentType(type ?? string.Empty) ? type! : CmsComponentTypes.Image;
            ViewBag.ComponentTypes = GetComponentTypes(requestedType);
            ViewBag.EventListSourceOptions = GetEventListSourceOptions(null);
            var model = new CmsComponentEditViewModel
            {
                PageId = pageId,
                Type = requestedType,
                Source = "query",
                Limit = 10
            };
            return View("ComponentCreate", model);
        }

        [HttpPost("pages/{pageId:guid}/components/create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateComponent(Guid pageId, CmsComponentEditViewModel model)
        {
            if (!IsSupportedComponentType(model.Type))
            {
                ModelState.AddModelError(nameof(model.Type), "Unsupported component type.");
            }

            ValidateComponentModel(model);

            var page = await _db.CmsPages.FirstOrDefaultAsync(p => p.Id == pageId);
            if (page == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ComponentTypes = GetComponentTypes(model.Type);
                ViewBag.EventListSourceOptions = GetEventListSourceOptions(model.Source);
                return View("ComponentCreate", model);
            }

            var now = DateTime.UtcNow;
            var position = model.Position > 0 ? model.Position : await GetNextComponentPositionAsync(pageId);
            var component = new CmsPageComponent
            {
                Id = Guid.NewGuid(),
                PageId = pageId,
                Type = model.Type,
                SettingsJson = BuildSettingsJson(model),
                Position = position,
                IsEnabled = model.IsEnabled,
                CreatedAt = now,
                UpdatedAt = now
            };

            _db.CmsPageComponents.Add(component);
            await TrySaveChangesAsync();

            return RedirectToAction(nameof(Edit), new { id = pageId });
        }

        [HttpGet("pages/{pageId:guid}/components/{id:guid}/edit")]
        public async Task<IActionResult> EditComponent(Guid pageId, Guid id)
        {
            var component = await _db.CmsPageComponents.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id && c.PageId == pageId);
            if (component == null)
            {
                return NotFound();
            }

            var model = MapComponent(component);
            ViewBag.ComponentTypes = GetComponentTypes(component.Type);
            ViewBag.EventListSourceOptions = GetEventListSourceOptions(model.Source);
            return View("ComponentEdit", model);
        }

        [HttpPost("pages/{pageId:guid}/components/{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditComponent(Guid pageId, Guid id, CmsComponentEditViewModel model)
        {
            var component = await _db.CmsPageComponents.FirstOrDefaultAsync(c => c.Id == id && c.PageId == pageId);
            if (component == null)
            {
                return NotFound();
            }

            if (!IsSupportedComponentType(model.Type))
            {
                ModelState.AddModelError(nameof(model.Type), "Unsupported component type.");
            }

            ValidateComponentModel(model);

            if (!ModelState.IsValid)
            {
                ViewBag.ComponentTypes = GetComponentTypes(model.Type);
                ViewBag.EventListSourceOptions = GetEventListSourceOptions(model.Source);
                return View("ComponentEdit", model);
            }

            component.Type = model.Type;
            component.SettingsJson = BuildSettingsJson(model);
            component.Position = model.Position;
            component.IsEnabled = model.IsEnabled;
            component.UpdatedAt = DateTime.UtcNow;

            await TrySaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = pageId });
        }

        [HttpPost("pages/{pageId:guid}/components/{id:guid}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComponent(Guid pageId, Guid id)
        {
            var component = await _db.CmsPageComponents.FirstOrDefaultAsync(c => c.Id == id && c.PageId == pageId);
            if (component == null)
            {
                return NotFound();
            }

            _db.CmsPageComponents.Remove(component);
            await TrySaveChangesAsync();
            return RedirectToAction(nameof(Edit), new { id = pageId });
        }

        private static List<CmsPageTreeItemViewModel> BuildTree(List<CmsPage> pages, Guid? parentId)
        {
            return pages
                .Where(p => p.ParentId == parentId)
                .OrderBy(p => p.Position)
                .ThenBy(p => p.Title)
                .Select(p => new CmsPageTreeItemViewModel
                {
                    Id = p.Id,
                    Title = p.Title,
                    Path = p.Path,
                    IsHomepage = p.IsHomepage,
                    IsPublished = p.IsPublished,
                    Position = p.Position,
                    Children = BuildTree(pages, p.Id)
                })
                .ToList();
        }

        private async Task PopulateParentOptionsAsync(CmsPageEditViewModel model, Guid? excludeId)
        {
            var pages = await _db.CmsPages.AsNoTracking().OrderBy(p => p.Position).ToListAsync();
            var options = new List<SelectListItem>();
            BuildParentOptionsRecursive(pages, options, null, 0, excludeId, model.ParentId);
            model.ParentOptions = options;
        }

        private static void BuildParentOptionsRecursive(
            List<CmsPage> pages,
            List<SelectListItem> options,
            Guid? parentId,
            int depth,
            Guid? excludeId,
            Guid? selectedId)
        {
            foreach (var page in pages.Where(p => p.ParentId == parentId).OrderBy(p => p.Position).ThenBy(p => p.Title))
            {
                if (excludeId.HasValue && page.Id == excludeId.Value)
                {
                    continue;
                }

                var prefix = depth == 0 ? string.Empty : new string('-', depth * 2) + " ";
                options.Add(new SelectListItem
                {
                    Value = page.Id.ToString(),
                    Text = prefix + page.Title,
                    Selected = selectedId.HasValue && page.Id == selectedId.Value
                });

                BuildParentOptionsRecursive(pages, options, page.Id, depth + 1, excludeId, selectedId);
            }
        }

        private async Task<CmsPage?> ResolveParentAsync(Guid? parentId)
        {
            if (!parentId.HasValue || parentId.Value == Guid.Empty)
            {
                return null;
            }

            return await _db.CmsPages.FirstOrDefaultAsync(p => p.Id == parentId.Value);
        }

        private async Task<string> EnsureUniqueSlugAsync(Guid? parentId, string baseSlug, Guid? excludeId)
        {
            var existing = await _db.CmsPages
                .AsNoTracking()
                .Where(p => p.ParentId == parentId && (!excludeId.HasValue || p.Id != excludeId.Value))
                .Select(p => p.Slug)
                .ToListAsync();

            var slug = baseSlug;
            var suffix = 2;
            while (existing.Any(s => string.Equals(s, slug, StringComparison.OrdinalIgnoreCase)))
            {
                slug = $"{baseSlug}-{suffix++}";
            }

            return slug;
        }

        private string BuildPath(CmsPage? parent, string slug)
        {
            var basePath = parent?.Path?.TrimEnd('/') ?? string.Empty;
            if (string.IsNullOrWhiteSpace(basePath))
            {
                return "/" + slug;
            }

            return basePath + "/" + slug;
        }

        private string? NormalizeCustomUrl(string? customUrl)
        {
            if (string.IsNullOrWhiteSpace(customUrl))
            {
                return null;
            }

            return _urlService.NormalizeCustomUrl(customUrl);
        }

        private async Task<bool> IsPathAllowedAsync(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || path == "/")
            {
                return true;
            }

            var segment = _urlService.GetTopLevelSegment(path);
            return !await _reservedRouteService.IsReservedSegmentAsync(segment);
        }

        private async Task<bool> IsPathUniqueAsync(string path, Guid? excludeId)
        {
            return !await _db.CmsPages
                .AsNoTracking()
                .AnyAsync(p => p.Path == path && (!excludeId.HasValue || p.Id != excludeId.Value));
        }

        private async Task<bool> IsDescendantAsync(Guid pageId, Guid possibleParentId)
        {
            var currentId = possibleParentId;
            while (true)
            {
                var parentId = await _db.CmsPages
                    .AsNoTracking()
                    .Where(p => p.Id == currentId)
                    .Select(p => p.ParentId)
                    .FirstOrDefaultAsync();

                if (!parentId.HasValue)
                {
                    return false;
                }

                if (parentId.Value == pageId)
                {
                    return true;
                }

                currentId = parentId.Value;
            }
        }

        private async Task<int> GetNextPositionAsync(Guid? parentId)
        {
            var max = await _db.CmsPages
                .Where(p => p.ParentId == parentId)
                .Select(p => (int?)p.Position)
                .MaxAsync();

            return (max ?? 0) + 1;
        }

        private async Task<int> GetNextComponentPositionAsync(Guid pageId)
        {
            var max = await _db.CmsPageComponents
                .Where(c => c.PageId == pageId)
                .Select(c => (int?)c.Position)
                .MaxAsync();

            return (max ?? 0) + 1;
        }

        private async Task UpdateDescendantPathsAsync(CmsPage page)
        {
            var children = await _db.CmsPages.Where(p => p.ParentId == page.Id).ToListAsync();
            foreach (var child in children)
            {
                if (!child.IsHomepage)
                {
                    if (string.IsNullOrWhiteSpace(child.CustomUrl))
                    {
                        child.Path = BuildPath(page, child.Slug);
                    }
                    else
                    {
                        child.Path = _urlService.NormalizeCustomUrl(child.CustomUrl);
                    }
                    child.UpdatedAt = DateTime.UtcNow;
                }

                await UpdateDescendantPathsAsync(child);
            }
        }

        private async Task DeletePageTreeAsync(CmsPage page)
        {
            var children = await _db.CmsPages.Where(p => p.ParentId == page.Id).ToListAsync();
            foreach (var child in children)
            {
                await DeletePageTreeAsync(child);
            }

            var components = await _db.CmsPageComponents.Where(c => c.PageId == page.Id).ToListAsync();
            if (components.Count > 0)
            {
                _db.CmsPageComponents.RemoveRange(components);
            }

            _db.CmsPages.Remove(page);
        }

        private bool IsSupportedComponentType(string type)
        {
            return type == CmsComponentTypes.Image ||
                   type == CmsComponentTypes.Banner ||
                   type == CmsComponentTypes.EventList;
        }

        private void ValidateComponentModel(CmsComponentEditViewModel model)
        {
            if (model.Type == CmsComponentTypes.Image && string.IsNullOrWhiteSpace(model.ImageUrl))
            {
                ModelState.AddModelError(nameof(model.ImageUrl), "Image URL is required.");
            }

            if (model.Type == CmsComponentTypes.Banner && string.IsNullOrWhiteSpace(model.Heading))
            {
                ModelState.AddModelError(nameof(model.Heading), "Heading is required.");
            }

            if (model.Type == CmsComponentTypes.EventList)
            {
                var source = NormalizeEventListSource(model.Source);
                if (source != "query" && source != "manual")
                {
                    ModelState.AddModelError(nameof(model.Source), "Source must be query or manual.");
                }

                if (source == "manual" && string.IsNullOrWhiteSpace(model.EventIds))
                {
                    ModelState.AddModelError(nameof(model.EventIds), "Event IDs are required for manual source.");
                }

                if (model.Limit.HasValue && (model.Limit < 1 || model.Limit > 50))
                {
                    ModelState.AddModelError(nameof(model.Limit), "Limit must be between 1 and 50.");
                }
            }
        }

        private string BuildSettingsJson(CmsComponentEditViewModel model)
        {
            object settings = model.Type switch
            {
                var t when t == CmsComponentTypes.Image => new CmsImageSettings
                {
                    Url = model.ImageUrl,
                    AltText = model.AltText,
                    Caption = model.Caption,
                    Alignment = model.Alignment,
                    LinkUrl = model.LinkUrl
                },
                var t when t == CmsComponentTypes.Banner => new CmsBannerSettings
                {
                    Heading = model.Heading,
                    Subheading = model.Subheading,
                    BackgroundImageUrl = model.BackgroundImageUrl,
                    CtaLabel = model.CtaLabel,
                    CtaUrl = model.CtaUrl
                },
                _ => new CmsEventListSettings
                {
                    Source = NormalizeEventListSource(model.Source),
                    EventIds = string.IsNullOrWhiteSpace(model.EventIds) ? null : model.EventIds.Trim(),
                    Category = string.IsNullOrWhiteSpace(model.Category) ? null : model.Category.Trim(),
                    Location = string.IsNullOrWhiteSpace(model.Location) ? null : model.Location.Trim(),
                    Limit = NormalizeEventListLimit(model.Limit)
                }
            };

            return JsonSerializer.Serialize(settings, JsonOptions);
        }

        private CmsComponentEditViewModel MapComponent(CmsPageComponent component)
        {
            var model = new CmsComponentEditViewModel
            {
                Id = component.Id,
                PageId = component.PageId,
                Type = component.Type,
                Position = component.Position,
                IsEnabled = component.IsEnabled
            };

            if (component.Type == CmsComponentTypes.Image)
            {
                var settings = Deserialize<CmsImageSettings>(component.SettingsJson);
                if (settings != null)
                {
                    model.ImageUrl = settings.Url;
                    model.AltText = settings.AltText;
                    model.Caption = settings.Caption;
                    model.Alignment = settings.Alignment;
                    model.LinkUrl = settings.LinkUrl;
                }
            }
            else if (component.Type == CmsComponentTypes.Banner)
            {
                var settings = Deserialize<CmsBannerSettings>(component.SettingsJson);
                if (settings != null)
                {
                    model.Heading = settings.Heading;
                    model.Subheading = settings.Subheading;
                    model.BackgroundImageUrl = settings.BackgroundImageUrl;
                    model.CtaLabel = settings.CtaLabel;
                    model.CtaUrl = settings.CtaUrl;
                }
            }
            else if (component.Type == CmsComponentTypes.EventList)
            {
                var settings = Deserialize<CmsEventListSettings>(component.SettingsJson);
                if (settings != null)
                {
                    model.Source = NormalizeEventListSource(settings.Source);
                    model.EventIds = settings.EventIds;
                    model.Category = settings.Category;
                    model.Location = settings.Location;
                    model.Limit = NormalizeEventListLimit(settings.Limit);
                }
                else
                {
                    model.Source = "query";
                    model.Limit = 10;
                }
            }

            return model;
        }

        private static T? Deserialize<T>(string json) where T : class
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            return JsonSerializer.Deserialize<T>(json);
        }

        private IEnumerable<SelectListItem> GetComponentTypes(string? selected)
        {
            var types = new[] { CmsComponentTypes.Image, CmsComponentTypes.Banner, CmsComponentTypes.EventList };
            return types.Select(t => new SelectListItem
            {
                Text = t,
                Value = t,
                Selected = string.Equals(t, selected, StringComparison.OrdinalIgnoreCase)
            });
        }

        private IEnumerable<SelectListItem> GetEventListSourceOptions(string? selected)
        {
            var normalized = NormalizeEventListSource(selected);
            return new[]
            {
                new SelectListItem { Text = "query", Value = "query", Selected = normalized == "query" },
                new SelectListItem { Text = "manual", Value = "manual", Selected = normalized == "manual" }
            };
        }

        private static string NormalizeEventListSource(string? source)
        {
            return string.Equals(source?.Trim(), "manual", StringComparison.OrdinalIgnoreCase)
                ? "manual"
                : "query";
        }

        private static int NormalizeEventListLimit(int? limit)
        {
            if (!limit.HasValue || limit.Value < 1)
            {
                return 10;
            }

            return limit.Value > 50 ? 50 : limit.Value;
        }

        private async Task<bool> TrySaveChangesAsync()
        {
            try
            {
                await _db.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "CMS save failed");
                ModelState.AddModelError(string.Empty, "Unable to save changes. Please check for duplicate URLs or slugs.");
                return false;
            }
        }
    }
}

