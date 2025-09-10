using GovFinance.Data;
using GovFinance.Models;
using Humanizer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Security.Claims;

namespace GovFinance.Controllers
{
    [Authorize(Roles = Roles.User)]
    public class MyIncomesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MyIncomesController(ApplicationDbContext db) => _db = db;

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
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var items = await _db.Incomes
                .Where(i => i.UserId == cid)
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
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == cid);
            if (item == null) return NotFound();

            return View(item);
        }

        // GET: /MyIncomes/Create
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            ViewBag.BaseIncomes = await _db.IncomeCategories
                .Where(x => x.UserId == cid)
                .OrderBy(x => x.Name)
                .ToListAsync();
            ViewBag.BaseExpenses = await _db.ExpenseCategories   // لو كنت تعرضها أيضًا
                    .Where(x => x.UserId == cid)
                    .OrderBy(x => x.Name)
                    .ToListAsync();
            return View(new Income { Date = DateOnly.FromDateTime(DateTime.Today) });
        }
       
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Amount,CollectedAmount,Source,Date,Notes,IncomeCategoryId")] Income model)
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            model.UserId = cid.Value;

            ModelState.Remove(nameof(Income.User));
            ModelState.Remove(nameof(Income.IncomeCategory));

            if (model.CollectedAmount < 0 || model.CollectedAmount > model.Amount)
                ModelState.AddModelError(nameof(Income.CollectedAmount), "المبلغ المقبوض يجب أن يكون بين 0 والمبلغ الكلي.");

            if (!ModelState.IsValid)
            {
                ViewBag.BaseIncomes = await _db.IncomeCategories
                    .Where(x => x.UserId == cid).OrderBy(x => x.Name).ToListAsync();
                return View(model);
            }

            // نضيف الدخل + (اختياريًا) نولّد مصروف مرتبط في نفس الترانزاكشن
            await using var tx = await _db.Database.BeginTransactionAsync();

            _db.Incomes.Add(model);

            // إذا الفئة المختارة مربوطة بفئة مصروف، أضف مصروف تلقائي
            if (model.IncomeCategoryId != 0)
            {
                var link = await _db.IncomeCategories
                    .Where(x => x.Id == model.IncomeCategoryId && x.UserId == cid)
                    .Select(x => x.LinkedExpenseCategoryId)
                    .FirstOrDefaultAsync();

                if (link != null)
                {
                    var expCat = await _db.ExpenseCategories
                        .FirstOrDefaultAsync(e => e.Id == link && e.UserId == cid);

                    if (expCat != null)
                    {
                        var expense = new Expense
                        {
                            UserId = cid.Value,
                            ExpenseCategoryId = expCat.Id,       // لو عندك الربط بالـId
                            Amount = expCat.DefaultAmount,       // المبلغ الافتراضي للفئة
                            PaidAmount = 0m,                     // مبدئيًا غير مدفوع
                            Date = model.Date,                   // نفس تاريخ الدخل
                            Notes = $"مصروف تلقائي مرتبط بدخل: {model.Source}"
                        };
                        var exists = await _db.Expenses.AnyAsync(e =>
                                e.UserId == cid &&
                                e.ExpenseCategoryId == expCat.Id &&
                                e.Date == model.Date &&
                                e.Amount == expCat.DefaultAmount &&
                                e.Notes!.Contains(model.Source));

                        if (!exists)
                        {
                            _db.Expenses.Add(expense);
                        }
                        _db.Expenses.Add(expense);
                    }
                }
            }

            await _db.SaveChangesAsync();
            await tx.CommitAsync();

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == cid);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, decimal? newPayment,
     [Bind("Id,Amount,Source,Date,Notes")] Income model)
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == cid);
            if (item == null) return NotFound();

            // إزالة تحقق حقول لا تأتي من الفورم
            ModelState.Remove(nameof(Income.UserId));
            ModelState.Remove(nameof(Income.User));
            ModelState.Remove(nameof(Income.IncomeCategory));

            if (newPayment.HasValue && newPayment.Value < 0)
                ModelState.AddModelError("newPayment", "الدفعة الجديدة يجب أن تكون رقمًا موجبًا.");

            if (!ModelState.IsValid)
                return View(item); // اعرض القيم الحالية مع أخطاء التحقق

            // تحديث الحقول القابلة للتعديل
            item.Amount = model.Amount;
            item.Source = model.Source;
            item.Date = model.Date;
            item.Notes = model.Notes;

            // تطبيق الدفعة الجديدة (إن وُجدت) مع ضبط الحدود
            if (newPayment.HasValue && newPayment.Value > 0)
                item.CollectedAmount += newPayment.Value;

            // لا تتجاوز ولا تنزل عن الصفر
            if (item.CollectedAmount > item.Amount) item.CollectedAmount = item.Amount;
            if (item.CollectedAmount < 0) item.CollectedAmount = 0;

            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes
                .FirstOrDefaultAsync(i => i.Id == id && i.UserId == cid);
            if (item == null) return NotFound();

            _db.Incomes.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public async Task<IActionResult> Deferred()
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var list = await _db.Incomes
                .Where(i => i.UserId == cid && i.Amount > i.CollectedAmount)
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

            var exp = await _db.Incomes
                .FirstOrDefaultAsync(e => e.Id == id && e.UserId == uid);
            if (exp is null) return NotFound();

            var remaining = Math.Max(0m, exp.Amount - exp.CollectedAmount);
            if (remaining <= 0m)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return Json(new { ok = true, alreadyPaid = true });

                return RedirectToAction(nameof(Deferred));
            }

            exp.CollectedAmount = exp.Amount; // تصفير المتبقي
            await _db.SaveChangesAsync();

            // إن كان الطلب AJAX أعِد JSON لتحديث الصف في نفس الصفحة
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new
                {
                    ok = true,
                    paid = exp.CollectedAmount,
                    paidFormatted = exp.CollectedAmount.ToString("N2", CultureInfo.InvariantCulture),
                    remaining = 0m,
                    remainingFormatted = "0.00"
                });
            }

            return RedirectToAction(nameof(Deferred));
        }
    }
}
