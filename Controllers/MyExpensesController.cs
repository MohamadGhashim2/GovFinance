using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GovFinance.Controllers
{
    // وصول المواطنين فقط
    [Authorize(Roles = Roles.Citizen)]
    public class MyExpensesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MyExpensesController(ApplicationDbContext db) => _db = db;

        // جلب معرف Citizen للمستخدم الحالي
        private async Task<int?> GetCitizenIdAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId is null) return null;

            return await _db.Citizens
                .Where(c => c.ApplicationUserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync();
        }

        // قائمة مصاريفي
        public async Task<IActionResult> Index()
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var items = await _db.Expenses
                .Where(e => e.CitizenId == cid)
                .OrderByDescending(e => e.Date)
                .ToListAsync();

            return View(items);
        }

        // إنشاء مصروف
        public IActionResult Create()
        {
            return View(new Expense
            {
                Date = DateOnly.FromDateTime(DateTime.Today)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Amount,Category,Date,Notes")] Expense model)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            model.CitizenId = cid.Value;                       // نحدد صاحب السجل
            ModelState.Remove(nameof(Expense.CitizenId));      // لأنه ينعطى من السيرفر
            ModelState.Remove(nameof(Expense.Citizen));        // navigation غير مربوط بالفورم

            if (!ModelState.IsValid)
                return View(model);

            _db.Expenses.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



    }
}
