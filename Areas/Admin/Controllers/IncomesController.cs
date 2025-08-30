using Microsoft.AspNetCore.Mvc;

namespace GovFinance.Areas.Admin.Controllers
{
    public class IncomesController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
