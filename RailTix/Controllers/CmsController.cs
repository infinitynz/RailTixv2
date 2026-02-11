using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using RailTix.Services.Cms;

namespace RailTix.Controllers
{
    public class CmsController : Controller
    {
        private readonly CmsPageRenderer _renderer;
        private readonly ICmsReservedRouteService _reservedRouteService;
        private readonly ICmsUrlService _urlService;

        public CmsController(
            CmsPageRenderer renderer,
            ICmsReservedRouteService reservedRouteService,
            ICmsUrlService urlService)
        {
            _renderer = renderer;
            _reservedRouteService = reservedRouteService;
            _urlService = urlService;
        }

        [HttpGet("{**path}", Order = 999)]
        public async Task<IActionResult> Page(string? path)
        {
            var normalized = _urlService.NormalizePath(path ?? "/");
            var segment = _urlService.GetTopLevelSegment(normalized);
            if (await _reservedRouteService.IsReservedSegmentAsync(segment))
            {
                return NotFound();
            }

            var page = await _renderer.GetPageByPathAsync(normalized);
            if (page == null)
            {
                return NotFound();
            }

            return View("Page", page);
        }
    }
}

