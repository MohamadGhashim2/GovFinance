using GovFinance.Services;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Globalization;

namespace GovFinance.Helpers
{
    public static class MoneyHtmlHelper
    {
        public static IHtmlContent Money(this IHtmlHelper html, decimal amount, string? format = null)
        {
            var provider = (ICurrencyProvider)html.ViewContext.HttpContext
                                                    .RequestServices
                                                    .GetService(typeof(ICurrencyProvider))!;
            // تنسيق بسيط: 2 منازل عشرية
            var text = (format ?? "N2");
            var num = amount.ToString(text, CultureInfo.InvariantCulture);
            // تقدر تعكس الترتيب لو بدك: $"{num} {provider.Symbol}"
            return new HtmlString($"{provider.Symbol} {num}");
        }
    }
}
