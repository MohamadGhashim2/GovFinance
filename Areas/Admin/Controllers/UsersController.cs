using GovFinance.Data;
using GovFinance.Models;
using GovFinance.Models.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GovFinance.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class UsersController : Controller
    {
        private readonly ApplicationDbContext _db;
        public UsersController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var list = await _db.Userrs
                .Include(c => c.ApplicationUser)
                .OrderBy(c => c.FullName)
                .ToListAsync();
            return View(list);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(User model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Userrs.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Users.FindAsync(id);
            if (c == null) return NotFound();
            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(User model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Userrs.FindAsync(id);
            if (c == null) return NotFound();
            _db.Userrs.Remove(c);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public async Task<IActionResult> Ledger(int id, DateOnly? start, DateOnly? end)
        {
            var user = await _db.Userrs
                .Include(c => c.ApplicationUser)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (user == null) return NotFound();

            var qIn = _db.Incomes.Where(i => i.UserId == id).AsNoTracking();
            var qEx = _db.Expenses.Where(e => e.UserId == id).AsNoTracking();

            if (start.HasValue) { qIn = qIn.Where(i => i.Date >= start.Value); qEx = qEx.Where(e => e.Date >= start.Value); }
            if (end.HasValue) { qIn = qIn.Where(i => i.Date <= end.Value); qEx = qEx.Where(e => e.Date <= end.Value); }

            var incomes = await qIn.OrderByDescending(i => i.Date).ThenBy(i => i.Id).ToListAsync();
            var expenses = await qEx.OrderByDescending(e => e.Date).ThenBy(e => e.Id).ToListAsync();

            var vm = new AdminUserLedgerVm
            {
                Id = user.Id,
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.ApplicationUser?.Email,
                Incomes = incomes,
                Expenses = expenses,
                TotalIncome = incomes.Sum(i => i.Amount),
                TotalExpense = expenses.Sum(e => e.Amount),
                Start = start?.ToString("yyyy-MM-dd"),
                End = end?.ToString("yyyy-MM-dd"),
            };

            return View(vm);
        }

        [HttpGet]
        public async Task<IActionResult> ExportLedgerCsv(int id, DateOnly? start, DateOnly? end, string type = "all")
        {
            var user = await _db.Userrs.Include(c => c.ApplicationUser).FirstOrDefaultAsync(c => c.Id == id);
            if (user == null) return NotFound();

            var qIn = _db.Incomes.Where(i => i.UserId == id).AsNoTracking();
            var qEx = _db.Expenses.Where(e => e.UserId == id).AsNoTracking();
            if (start.HasValue) { qIn = qIn.Where(i => i.Date >= start.Value); qEx = qEx.Where(e => e.Date >= start.Value); }
            if (end.HasValue) { qIn = qIn.Where(i => i.Date <= end.Value); qEx = qEx.Where(e => e.Date <= end.Value); }

            var sb = new System.Text.StringBuilder();
            if (type.Equals("incomes", StringComparison.OrdinalIgnoreCase) || type.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                sb.AppendLine("Type,Date,Amount,Source,Notes");
                foreach (var i in await qIn.OrderBy(i => i.Date).ThenBy(i => i.Id).ToListAsync())
                    sb.AppendLine($"Income,{i.Date:yyyy-MM-dd},{i.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)},{Csv(i.Source)},{Csv(i.Notes)}");
            }

            if (type.Equals("expenses", StringComparison.OrdinalIgnoreCase) || type.Equals("all", StringComparison.OrdinalIgnoreCase))
            {
                if (!type.Equals("all", StringComparison.OrdinalIgnoreCase)) // header if only expenses
                    sb.AppendLine("Type,Date,Amount,Category,Notes");

                foreach (var e in await qEx.OrderBy(e => e.Date).ThenBy(e => e.Id).ToListAsync())
                    sb.AppendLine($"Expense,{e.Date:yyyy-MM-dd},{e.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture)},,{Csv(e.Notes)}");
            }

            var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"ledger_{user.UserId}_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);

            static string Csv(string? s)
            {
                if (string.IsNullOrEmpty(s)) return "";
                s = s.Replace("\"", "\"\"");
                return $"\"{s}\"";
            }
        }
    }
}
