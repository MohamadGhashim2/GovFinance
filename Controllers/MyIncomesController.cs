using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GovFinance.Controllers
{
    [Authorize(Roles = Roles.Citizen)]
    public class MyIncomesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public MyIncomesController(ApplicationDbContext db) => _db = db;

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

            var items = await _db.Incomes
                .Where(i => i.CitizenId == cid)
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

        public async Task<IActionResult> Details(int id)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes.AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == id && i.CitizenId == cid);
            if (item == null) return NotFound();

            return View(item);
        }

        public IActionResult Create()
        {
            return View(new Income { Date = DateOnly.FromDateTime(DateTime.Today) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Amount,Source,Date,Notes")] Income model)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            model.CitizenId = cid.Value;
            ModelState.Remove(nameof(Income.CitizenId));
            ModelState.Remove(nameof(Income.Citizen));

            if (!ModelState.IsValid) return View(model);

            _db.Incomes.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.CitizenId == cid);
            if (item == null) return NotFound();

            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Amount,Source,Date,Notes")] Income model)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.CitizenId == cid);
            if (item == null) return NotFound();

            ModelState.Remove(nameof(Income.CitizenId));
            ModelState.Remove(nameof(Income.Citizen));
            if (!ModelState.IsValid) return View(model);

            item.Amount = model.Amount;
            item.Source = model.Source;
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

            var item = await _db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.CitizenId == cid);
            if (item == null) return NotFound();

            _db.Incomes.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
