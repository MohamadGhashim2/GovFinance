using GovFinance.Services;
using Microsoft.AspNetCore.Mvc;

namespace GovFinance.Controllers
{
    public class SettingsController : Controller
    {
        private readonly ICurrencyProvider _currency;
        public SettingsController(ICurrencyProvider currency) => _currency = currency;

        [HttpGet]
        public IActionResult SetCurrency(string code, string? returnUrl = null)
        {
            if (!_currency.IsSupported(code))
                code = "TRY";

            Response.Cookies.Append(CookieCurrencyProvider.CookieName, code, new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddYears(1),
                IsEssential = true,
                HttpOnly = false,
                Secure = true,
                SameSite = SameSiteMode.Lax
            });

            if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction("Index", "Home");
        }
    }
}
