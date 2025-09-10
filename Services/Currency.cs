using Microsoft.AspNetCore.Http;

namespace GovFinance.Services
{
    public interface ICurrencyProvider
    {
        string Code { get; }
        string Symbol { get; }
        bool IsSupported(string code);
        IReadOnlyDictionary<string, string> All { get; }
    }

    public sealed class CookieCurrencyProvider : ICurrencyProvider
    {
        public const string CookieName = "app.currency";

        private static readonly Dictionary<string, string> _map = new()
        {
            ["TRY"] = "₺",  // ليرة تركية
            ["USD"] = "$",  // دولار
            ["SAR"] = "﷼",  // ريال سعودي
            ["EUR"] = "€",  // يورو
            ["SYP"] = "SYP",  // ليرة سورية
        };

        private readonly IHttpContextAccessor _http;

        public CookieCurrencyProvider(IHttpContextAccessor http) => _http = http;

        public string Code
        {
            get
            {
                var ctx = _http.HttpContext;
                if (ctx?.Request.Cookies.TryGetValue(CookieName, out var code) == true
                    && _map.ContainsKey(code))
                    return code;

                return "TRY"; // الافتراضي
            }
        }

        public string Symbol => _map.TryGetValue(Code, out var s) ? s : "₺";

        public bool IsSupported(string code) => !string.IsNullOrWhiteSpace(code) && _map.ContainsKey(code);

        public IReadOnlyDictionary<string, string> All => _map;
    }
}
