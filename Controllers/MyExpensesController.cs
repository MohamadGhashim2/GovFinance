using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace GovFinance.Controllers
{
    [Authorize(Roles = Roles.User)]
    public class MyExpensesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MyExpensesController(ApplicationDbContext db) => _db = db;

        private async Task<int?> GetUserIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return null;

            return await _db.Userrs
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            var items = await _db.Expenses
                .Where(i => i.UserId == uid)
                .OrderByDescending(i => i.Date)
                .AsNoTracking()
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var firstOfMonth = new DateOnly(today.Year, today.Month, 1);
            var firstOfYear = new DateOnly(today.Year, 1, 1);

            ViewBag.TotalAll = items.Sum(i => i.Amount);
            ViewBag.TotalMonth = items.Where(i => i.Date >= firstOfMonth && i.Date <= today).Sum(i => i.Amount);
            ViewBag.TotalYear = items.Where(i => i.Date >= firstOfYear && i.Date <= today).Sum(i => i.Amount);

            return View(items);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            var item = await _db.Expenses.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);
            if (item == null) return NotFound();

            return View(item);
        }

        // GET: /MyExpenses/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            ViewBag.BaseExpenses = await _db.ExpenseCategories
                .Where(x => x.UserId == uid)
                .OrderBy(x => x.Name)
                .ToListAsync();

            return View(new Expense { Date = DateOnly.FromDateTime(DateTime.Today) });
        }

        // POST: /MyExpenses/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Amount,PaidAmount,Source,Date,Notes,ExpenseCategoryId")] Expense model)
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            model.UserId = uid.Value;

            ModelState.Remove(nameof(Expense.User));
            ModelState.Remove(nameof(Expense.ExpenseCategory));

            if (model.PaidAmount < 0 || model.PaidAmount > model.Amount)
                ModelState.AddModelError(nameof(Expense.PaidAmount), "المبلغ المدفوع يجب أن يكون بين 0 والمبلغ الكلي.");

            if (!ModelState.IsValid)
            {
                ViewBag.BaseExpenses = await _db.ExpenseCategories
                    .Where(x => x.UserId == uid).OrderBy(x => x.Name).ToListAsync();
                return View(model);
            }

            _db.Expenses.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            var item = await _db.Expenses
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            decimal? newPayment,
            [Bind("Id,Amount,Source,Date,Notes")] Expense model)
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            var item = await _db.Expenses.FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);
            if (item == null) return NotFound();

            // إزالة تحقق حقول لا تأتي من الفورم
            ModelState.Remove(nameof(Expense.UserId));
            ModelState.Remove(nameof(Expense.User));
            ModelState.Remove(nameof(Expense.ExpenseCategory));

            if (newPayment.HasValue && newPayment.Value < 0)
                ModelState.AddModelError("newPayment", "الدفعة الجديدة يجب أن تكون رقمًا موجبًا.");

            if (!ModelState.IsValid)
                return View(item);

            // تحديث الحقول
            item.Amount = model.Amount;
            item.Source = model.Source;
            item.Date = model.Date;
            item.Notes = model.Notes;

            // تطبيق الدفعة الجزئية
            if (newPayment.HasValue && newPayment.Value > 0)
                item.PaidAmount += newPayment.Value;

            // حدود منطقية
            if (item.PaidAmount > item.Amount) item.PaidAmount = item.Amount;
            if (item.PaidAmount < 0) item.PaidAmount = 0;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            var item = await _db.Expenses
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == uid);
            if (item == null) return NotFound();

            _db.Expenses.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Deferred()
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            var list = await _db.Expenses
                .Where(i => i.UserId == uid && i.Amount > i.PaidAmount)
                .OrderBy(i => i.Date)
                .AsNoTracking()
                .ToListAsync();

            return View(list);
        }

        // ========= دفع كامل (فوري) =========
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(int id)
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();

            var exp = await _db.Expenses
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == uid);
            if (exp is null) return NotFound();

            var remaining = Math.Max(0m, exp.Amount - exp.PaidAmount);
            if (remaining <= 0m)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { ok = true, alreadyPaid = true });

                return RedirectToAction(nameof(Deferred));
            }

            exp.PaidAmount = exp.Amount; // تصفير المتبقي
            await _db.SaveChangesAsync();

            // إن كان الطلب AJAX أعِد JSON لتحديث الصف في نفس الصفحة
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    ok = true,
                    paid = exp.PaidAmount,
                    paidFormatted = exp.PaidAmount.ToString("N2", CultureInfo.InvariantCulture),
                    remaining = 0m,
                    remainingFormatted = "0.00"
                });
            }

            return RedirectToAction(nameof(Deferred));
        }
    }
}
