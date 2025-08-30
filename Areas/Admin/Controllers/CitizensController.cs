using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GovFinance.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class CitizensController : Controller
    {
        private readonly ApplicationDbContext _db;
        public CitizensController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index()
        {
            var list = await _db.Citizens
                .Include(c => c.ApplicationUser)
                .OrderBy(c => c.FullName)
                .ToListAsync();
            return View(list);
        }

        public IActionResult Create() => View();

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Citizen model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Citizens.Add(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var c = await _db.Citizens.FindAsync(id);
            if (c == null) return NotFound();
            return View(c);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Citizen model)
        {
            if (!ModelState.IsValid) return View(model);
            _db.Update(model);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var c = await _db.Citizens.FindAsync(id);
            if (c == null) return NotFound();
            _db.Citizens.Remove(c);
            await _db.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
    }
}
