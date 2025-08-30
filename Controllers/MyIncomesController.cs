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
            var incomes = await _db.Incomes
                .Where(i => i.CitizenId == cid)
                .OrderByDescending(i => i.Date)
                .ToListAsync();
            return View(incomes);
        }

        public IActionResult Create() => View(new Income { Date = DateOnly.FromDateTime(DateTime.Today) });

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Income model)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            if (!ModelState.IsValid) return View(model);

            model.CitizenId = cid.Value;
            _db.Incomes.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes.FirstOrDefaultAsync(x => x.Id == id && x.CitizenId == cid);
            if (item == null) return NotFound();
            return View(item);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Income model)
        {
            var cid = await GetCitizenIdAsync();
            if (cid == null) return Forbid();

            var item = await _db.Incomes.FirstOrDefaultAsync(x => x.Id == model.Id && x.CitizenId == cid);
            if (item == null) return NotFound();

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

            var item = await _db.Incomes.FirstOrDefaultAsync(x => x.Id == id && x.CitizenId == cid);
            if (item == null) return NotFound();

            _db.Incomes.Remove(item);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
