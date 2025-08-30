using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using GovFinance.Models;

namespace GovFinance.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class DashboardController : Controller
    {
        public IActionResult Index() => View();
    }
}
