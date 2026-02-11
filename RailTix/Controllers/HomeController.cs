using Microsoft.AspNetCore.Mvc;
using RailTix.Services.Cms;
using System.Threading.Tasks;

namespace RailTix.Controllers
{
    public class HomeController : Controller
    {
        private readonly CmsPageRenderer _cmsRenderer;

        public HomeController(CmsPageRenderer cmsRenderer)
        {
            _cmsRenderer = cmsRenderer;
        }

        public async Task<IActionResult> Index()
        {
            var page = await _cmsRenderer.GetHomepageAsync();
            if (page != null)
            {
                return View("~/Views/Cms/Page.cshtml", page);
            }

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View();
        }
    }
}


