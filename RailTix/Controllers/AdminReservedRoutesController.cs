using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RailTix.Data;
using RailTix.Models.Domain;
using RailTix.Models.ViewModels.Cms.Admin;
using RailTix.Services.Cms;

namespace RailTix.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("account/admin/content/reserved-routes")]
    public class AdminReservedRoutesController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ICmsUrlService _urlService;
        private readonly ICmsReservedRouteService _reservedRouteService;
        private readonly ILogger<AdminReservedRoutesController> _logger;

        public AdminReservedRoutesController(
            ApplicationDbContext db,
            ICmsUrlService urlService,
            ICmsReservedRouteService reservedRouteService,
            ILogger<AdminReservedRoutesController> logger)
        {
            _db = db;
            _urlService = urlService;
            _reservedRouteService = reservedRouteService;
            _logger = logger;
        }

        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var routes = await _db.CmsReservedRoutes
                .AsNoTracking()
                .OrderBy(r => r.Segment)
                .Select(r => new CmsReservedRouteEditViewModel
                {
                    Id = r.Id,
                    Segment = r.Segment,
                    IsActive = r.IsActive
                })
                .ToListAsync();

            return View(new CmsReservedRouteListViewModel { Routes = routes });
        }

        [HttpGet("create")]
        public IActionResult Create()
        {
            return View(new CmsReservedRouteEditViewModel());
        }

        [HttpPost("create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CmsReservedRouteEditViewModel model)
        {
            var segment = _urlService.NormalizeSegment(model.Segment);
            if (string.IsNullOrWhiteSpace(segment))
            {
                ModelState.AddModelError(nameof(model.Segment), "Segment is required.");
            }

            var exists = await _db.CmsReservedRoutes.AsNoTracking().AnyAsync(r => r.Segment == segment);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Segment), "That segment already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var now = DateTime.UtcNow;
            _db.CmsReservedRoutes.Add(new CmsReservedRoute
            {
                Id = Guid.NewGuid(),
                Segment = segment,
                IsActive = model.IsActive,
                CreatedAt = now,
                UpdatedAt = now
            });

            if (!await TrySaveChangesAsync())
            {
                return View(model);
            }

            _reservedRouteService.InvalidateCache();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("{id:guid}/edit")]
        public async Task<IActionResult> Edit(Guid id)
        {
            var route = await _db.CmsReservedRoutes.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
            if (route == null)
            {
                return NotFound();
            }

            return View(new CmsReservedRouteEditViewModel
            {
                Id = route.Id,
                Segment = route.Segment,
                IsActive = route.IsActive
            });
        }

        [HttpPost("{id:guid}/edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CmsReservedRouteEditViewModel model)
        {
            var route = await _db.CmsReservedRoutes.FirstOrDefaultAsync(r => r.Id == id);
            if (route == null)
            {
                return NotFound();
            }

            var segment = _urlService.NormalizeSegment(model.Segment);
            if (string.IsNullOrWhiteSpace(segment))
            {
                ModelState.AddModelError(nameof(model.Segment), "Segment is required.");
            }

            var exists = await _db.CmsReservedRoutes.AsNoTracking()
                .AnyAsync(r => r.Segment == segment && r.Id != route.Id);
            if (exists)
            {
                ModelState.AddModelError(nameof(model.Segment), "That segment already exists.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            route.Segment = segment;
            route.IsActive = model.IsActive;
            route.UpdatedAt = DateTime.UtcNow;

            if (!await TrySaveChangesAsync())
            {
                return View(model);
            }

            _reservedRouteService.InvalidateCache();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("{id:guid}/delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            var route = await _db.CmsReservedRoutes.FirstOrDefaultAsync(r => r.Id == id);
            if (route == null)
            {
                return NotFound();
            }

            _db.CmsReservedRoutes.Remove(route);
            if (!await TrySaveChangesAsync())
            {
                return RedirectToAction(nameof(Index));
            }

            _reservedRouteService.InvalidateCache();
            return RedirectToAction(nameof(Index));
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
                _logger.LogError(ex, "CMS reserved route save failed");
                ModelState.AddModelError(string.Empty, "Unable to save changes.");
                return false;
            }
        }
    }
}

