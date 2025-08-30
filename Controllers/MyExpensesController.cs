using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GovFinance.Controllers
{
    [Authorize(Roles = Roles.Citizen)]
    public class MyExpensesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MyExpensesController(ApplicationDbContext db) => _db = db;

        private async Task<int?> GetCitizenIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return null;

            return await _db.Citizens
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        public async Task<IActionResult> Index()
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var items = await _db.Expenses
                .Where(e => e.CitizenId == cid)
                .OrderByDescending(e => e.Date)
                .AsNoTracking()
                .ToListAsync();

            // مجاميع سريعة
            var today = DateOnly.FromDateTime(DateTime.Today);
            var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
            var firstOfYear = new DateOnly(today.Year, 1, 1);

            ViewBag.TotalAll = items.Sum(e => e.Amount);
            ViewBag.TotalMonth = items.Where(e => e.Date >= firstOfMonth && e.Date <= today).Sum(e => e.Amount);
            ViewBag.TotalYear = items.Where(e => e.Date >= firstOfYear && e.Date <= today).Sum(e => e.Amount);

            return View(items);
        }

        public async Task<IActionResult> Details(int id)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Expenses
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id && e.CitizenId == cid);
            if (item == null) return NotFound();

            return View(item);
        }

        public IActionResult Create()
        {
            return View(new Expense { Date = DateOnly.FromDateTime(DateTime.Today) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Amount,Category,Date,Notes")] Expense model)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            model.CitizenId = cid.Value;
            ModelState.Remove(nameof(Expense.CitizenId));
            ModelState.Remove(nameof(Expense.Citizen));

            if (!ModelState.IsValid) return View(model);

            _db.Expenses.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CitizenId == cid);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Amount,Category,Date,Notes")] Expense model)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            // احضر السجل وتأكد أنه يخص هذا المواطن
            var item = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CitizenId == cid);
            if (item == null) return NotFound();

            // تجاهل التحقق لحقول لا تأتي من الفورم
            ModelState.Remove(nameof(Expense.CitizenId));
            ModelState.Remove(nameof(Expense.Citizen));

            if (!ModelState.IsValid)
                return View(model);

            // حدّث الحقول القابلة للتعديل فقط
            item.Amount = model.Amount;
            item.Category = model.Category;
            item.Date = model.Date;
            item.Notes = model.Notes;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.CitizenId == cid);
            if (item == null) return NotFound();

            _db.Expenses.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
