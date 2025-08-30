using Microsoft.AspNetCore.Mvc;

namespace GovFinance.Controllers
{
    public class AuthController : Controller
    {
        // صفحة الزرين (مواطن/حكومة)
        public IActionResult ChooseRole() => View();
    }
}
