using Microsoft.AspNetCore.Mvc;

namespace GovFinance.Controllers
{
    public class AuthController : Controller
    {
        
        public IActionResult ChooseRole() => View();
    }
}
