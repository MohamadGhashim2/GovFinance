using System.Globalization;
using System.Text;
using GovFinance.Data;
using GovFinance.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GovFinance.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = Roles.Admin)]
    public class ExpensesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public ExpensesController(ApplicationDbContext db) => _db = db;

        public async Task<IActionResult> Index(DateOnly? start, DateOnly? end, string? q)
        {
            var query = _db.Expenses
                .Include(e => e.User)
                .ThenInclude(c => c.ApplicationUser)
                .AsNoTracking()
                .AsQueryable();

            if (start.HasValue)
                query = query.Where(e => e.Date >= start.Value);
            if (end.HasValue)
                query = query.Where(e => e.Date <= end.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(e =>
                    (e.User.FullName != null && EF.Functions.Like(e.User.FullName, $"%{q}%")) ||
                    (e.User.UserId != null && EF.Functions.Like(e.User.UserId, $"%{q}%")) ||
                    (e.User.ApplicationUser.Email != null && EF.Functions.Like(e.User.ApplicationUser.Email, $"%{q}%"))
                );
            }

            var items = await query
                .OrderByDescending(e => e.Date).ThenBy(e => e.Id)
                .ToListAsync();

            ViewBag.Start = start?.ToString("yyyy-MM-dd");
            ViewBag.End = end?.ToString("yyyy-MM-dd");
            ViewBag.Q = q ?? "";
            ViewBag.Total = items.Sum(x => x.Amount);

            return View(items);
        }

        public async Task<IActionResult> ExportCsv(DateOnly? start, DateOnly? end, string? q)
        {
            var query = _db.Expenses
                .Include(e => e.User)
                .ThenInclude(c => c.ApplicationUser)
                .AsNoTracking()
                .AsQueryable();

            if (start.HasValue)
                query = query.Where(e => e.Date >= start.Value);
            if (end.HasValue)
                query = query.Where(e => e.Date <= end.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(e =>
                    (e.User.FullName != null && EF.Functions.Like(e.User.FullName, $"%{q}%")) ||
                    (e.User.UserId != null && EF.Functions.Like(e.User.UserId, $"%{q}%")) ||
                    (e.User.ApplicationUser.Email != null && EF.Functions.Like(e.User.ApplicationUser.Email, $"%{q}%"))
                );
            }

            var items = await query
                .OrderBy(e => e.Date).ThenBy(e => e.Id)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Date,UserId,FullName,Email,Amount,Category,Notes");
            foreach (var e in items)
            {
                var line = string.Join(",", new[]
                {
                    e.Date.ToString("yyyy-MM-dd"),
                    Csv(e.User?.UserId),
                    Csv(e.User?.FullName),
                    Csv(e.User?.ApplicationUser?.Email),
                    e.Amount.ToString(CultureInfo.InvariantCulture),
                   
                    Csv(e.Notes)
                });
                sb.AppendLine(line);
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"expenses_{DateTime.Now:yyyyMMddHHmmss}.csv";
            return File(bytes, "text/csv", fileName);
        }

        private static string Csv(string? s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            s = s.Replace("\"", "\"\"");
            return $"\"{s}\"";
        }
    }
}
