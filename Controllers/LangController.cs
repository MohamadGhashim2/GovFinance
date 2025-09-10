using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

public class LangController : Controller
{
    [HttpPost]
    public IActionResult Set(string culture, string? returnUrl = null)
    {
        var supported = new[] { "ar", "en" };
        if (!supported.Contains(culture)) culture = "ar";

        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) });

        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            return LocalRedirect(returnUrl);

        var referer = Request.Headers["Referer"].ToString();
        if (!string.IsNullOrEmpty(referer) && Url.IsLocalUrl(new Uri(referer).PathAndQuery))
            return Redirect(referer);

        return RedirectToAction("Index", "Home");
    }
}
