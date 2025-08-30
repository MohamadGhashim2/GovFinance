using System.Diagnostics;
using System.Security.Claims;
using GovFinance.Data;
using GovFinance.Models;
using GovFinance.Models.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GovFinance.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ApplicationDbContext db, ILogger<HomeController> logger)
        {
            _db = db;
            _logger = logger;
        }

        private async Task<int?> GetCitizenIdAsync()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return null;

            return await _db.Citizens
                .Where(c => c.ApplicationUserId == uid)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<IActionResult> Index()
        {
            var vm = new HomeIndexVm();

            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole(Roles.Citizen))
                {
                    vm.IsCitizen = true;

                    var cid = await GetCitizenIdAsync();
                    if (cid != null)
                    {
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

                        vm.IncomeMonth = await _db.Incomes
                            .Where(i => i.CitizenId == cid && i.Date >= firstOfMonth && i.Date <= today)
                            .SumAsync(i => (decimal?)i.Amount) ?? 0m;

                        vm.ExpenseMonth = await _db.Expenses
                            .Where(e => e.CitizenId == cid && e.Date >= firstOfMonth && e.Date <= today)
                            .SumAsync(e => (decimal?)e.Amount) ?? 0m;
                    }
                }
                else if (User.IsInRole(Roles.Admin))
                {
                    vm.IsAdmin = true;
                    // ممكن لاحقًا نعرض كروت إحصائية للإدارة هنا.
                }
            }

            return View(vm);
        }
        public IActionResult Terms() => View();
        public IActionResult Accessibility() => View();
        public IActionResult Privacy() => View();
    }
}
