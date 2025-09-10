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

        private async Task<int?> GetUserIdAsync()
        {
            var uid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (uid is null) return null;

            return await _db.Userrs
                .Where(c => c.ApplicationUserId == uid)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        // يمكن التحكم بعدد العناصر + فلترة النوع + نص البحث
        // /?take=10&type=all|income|expense&q=نص
        public async Task<IActionResult> Index( string type = "all", string? q = null)
        {
            var vm = new HomeIndexVm();
            

            if (User?.Identity?.IsAuthenticated == true)
            {
                if (User.IsInRole(Roles.User))
                {
                    vm.IsUser = true;

                    var uid = await GetUserIdAsync();
                    if (uid != null)
                    {
                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var firstOfMonth = new DateOnly(today.Year, today.Month, 1);

                        // الدخل
                        vm.IncomeMonth = await _db.Incomes
                            .Where(i => i.UserId == uid && i.Date >= firstOfMonth && i.Date <= today)
                            .SumAsync(i => (decimal?)i.Amount) ?? 0m;

                        vm.DeferredIncomeMonth = await _db.Incomes
                            .Where(i => i.UserId == uid && i.Date >= firstOfMonth && i.Date <= today)
                            .SumAsync(i => (decimal?)(i.Amount - i.CollectedAmount)) ?? 0m;

                        // المصروف
                        vm.ExpenseMonth = await _db.Expenses
                            .Where(e => e.UserId == uid && e.Date >= firstOfMonth && e.Date <= today)
                            .SumAsync(e => (decimal?)e.Amount) ?? 0m;

                        vm.DeferredExpenseMonth = await _db.Expenses
                            .Where(e => e.UserId == uid && e.Date >= firstOfMonth && e.Date <= today)
                            .SumAsync(e => (decimal?)(e.Amount - e.PaidAmount)) ?? 0m;

                

                        // بناء "آخر الحركات"
                        IQueryable<LastEntryVm> incomesQ = _db.Incomes
                            .Where(i => i.UserId == uid)
                            .Select(i => new LastEntryVm
                            {
                                Type = "دخل",
                                Source = i.Source,
                                Amount = i.Amount,
                                PaidOrCollected = i.CollectedAmount,
                                Outstanding = i.Amount - i.CollectedAmount,
                                Date = i.Date,
                                Notes = i.Notes
                            });

                        IQueryable<LastEntryVm> expensesQ = _db.Expenses
                            .Where(e => e.UserId == uid)
                            .Select(e => new LastEntryVm
                            {
                                Type = "مصروف",
                                Source = e.Source,
                                Amount = e.Amount,
                                PaidOrCollected = e.PaidAmount,
                                Outstanding = e.Amount - e.PaidAmount,
                                Date = e.Date,
                                Notes = e.Notes
                            });

                        if (!string.IsNullOrWhiteSpace(q))
                        {
                            q = q.Trim();
                            incomesQ = incomesQ.Where(x => x.Source.Contains(q) || (x.Notes ?? "").Contains(q));
                            expensesQ = expensesQ.Where(x => x.Source.Contains(q) || (x.Notes ?? "").Contains(q));
                        }

                        IQueryable<LastEntryVm> entriesQ = type switch
                        {
                            "income" => incomesQ,
                            "expense" => expensesQ,
                            _ => incomesQ.Concat(expensesQ)
                        };

                        vm.LastEntries = await entriesQ
                            .OrderByDescending(x => x.Date)
                            .ToListAsync();

                        
                        ViewBag.Type = type;
                        ViewBag.Query = q;
                    }
                }
                else if (User.IsInRole(Roles.Admin))
                {
                    vm.IsAdmin = true;
                }
            }

            return View(vm);
        }

        public IActionResult Terms() => View();
        public IActionResult Accessibility() => View();
        public IActionResult Privacy() => View();
    }
}
