using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GovFinance.Controllers
{
    [Authorize(Roles = Roles.User)]
    public class IncomeCategoriesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public IncomeCategoriesController(ApplicationDbContext db) => _db = db;

        private async Task<int?> GetUserIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return null;

            return await _db.Userrs
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        // قائمة المصادر الثابتة
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var list = await _db.IncomeCategories
                .Where(x => x.UserId == cid)
                .OrderBy(x => x.Name)
                .AsNoTracking()
                .ToListAsync();

            return View(list);
        }

        [HttpGet]

        public async Task<IActionResult> Create(bool asPartial = false, string? returnUrl = null)
        {
            var uid = await GetUserIdAsync();
            if (uid == null) return Forbid();
            // حمّل مصروفات المستخدم للربط
            ViewBag.ExpenseCats = await _db.ExpenseCategories
                .Where(e => e.UserId == uid)              
                .OrderBy(e => e.Name)
                .ToListAsync();
            ViewBag.ReturnUrl = string.IsNullOrWhiteSpace(returnUrl) ? null : returnUrl;
            var model = new IncomeCategory { DefaultAmount = 0 };
            return asPartial ? PartialView("_CreateForm", model) : View(model);
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(IncomeCategory model, bool asPartial = false, string? returnUrl = null)
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            // اربط بالمواطن الحالي
            model.UserId = cid.Value;

            ModelState.Remove(nameof(IncomeCategory.User));
            // التحقّق من الربط (إن اختير)
            if (model.LinkedExpenseCategoryId.HasValue)
            {
                var ok = await _db.ExpenseCategories
                    .AnyAsync(e => e.Id == model.LinkedExpenseCategoryId && e.UserId == cid);
                if (!ok)
                    ModelState.AddModelError(nameof(IncomeCategory.LinkedExpenseCategoryId), "مصروف مرتبط غير صالح.");
            }
            // تحقق أساسي
            if (string.IsNullOrWhiteSpace(model.Name))
                ModelState.AddModelError(nameof(model.Name), "الاسم مطلوب.");
            if (model.DefaultAmount < 0)
                ModelState.AddModelError(nameof(model.DefaultAmount), "المبلغ الافتراضي يجب أن يكون موجبًا.");

            // منع التكرار
            if (ModelState.IsValid)
            {
                var exists = await _db.IncomeCategories
                    .AnyAsync(x => x.UserId == cid && x.Name == model.Name.Trim());
                if (exists)
                    ModelState.AddModelError(nameof(model.Name), "هذا الاسم موجود مسبقًا.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.ExpenseCats = await _db.ExpenseCategories
                    .Where(e => e.UserId == cid).OrderBy(e => e.Name).ToListAsync();
                return asPartial ? PartialView("_CreateForm", model) : View(model);
            }

            model.Name = model.Name.Trim();
            _db.IncomeCategories.Add(model);
            await _db.SaveChangesAsync();

            if (asPartial)
                return Json(new { ok = true, id = model.Id, name = model.Name, amount = model.DefaultAmount });

            if (!string.IsNullOrWhiteSpace(returnUrl))
                return LocalRedirect(returnUrl);

            return RedirectToAction(nameof(Index));
        }

        // (اختياري) حذف بسيط
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var cid = await GetUserIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.IncomeCategories
                .FirstOrDefaultAsync(x => x.Id == id && x.UserId == cid);
            if (item == null) return NotFound();

            _db.IncomeCategories.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
