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
    public class IncomesController : Controller
    {
        private readonly ApplicationDbContext _db;
        public IncomesController(ApplicationDbContext db) => _db = db;

        // GET: Admin/Incomes
        public async Task<IActionResult> Index(DateOnly? start, DateOnly? end, string? q)
        {
            var query = _db.Incomes
                .Include(i => i.User)
                .ThenInclude(c => c.ApplicationUser)
                .AsNoTracking()
                .AsQueryable();

            if (start.HasValue)
                query = query.Where(i => i.Date >= start.Value);
            if (end.HasValue)
                query = query.Where(i => i.Date <= end.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(i =>
                    (i.User.FullName != null && EF.Functions.Like(i.User.FullName, $"%{q}%")) ||
                    (i.User.UserId != null && EF.Functions.Like(i.User.UserId, $"%{q}%")) ||
                    (i.User.ApplicationUser.Email != null && EF.Functions.Like(i.User.ApplicationUser.Email, $"%{q}%"))
                );
            }

            var items = await query
                .OrderByDescending(i => i.Date).ThenBy(i => i.Id)
                .ToListAsync();

            ViewBag.Start = start?.ToString("yyyy-MM-dd");
            ViewBag.End = end?.ToString("yyyy-MM-dd");
            ViewBag.Q = q ?? "";
            ViewBag.Total = items.Sum(x => x.Amount);

            return View(items);
        }

        // GET: Admin/Incomes/ExportCsv
        public async Task<IActionResult> ExportCsv(DateOnly? start, DateOnly? end, string? q)
        {
            var query = _db.Incomes
                .Include(i => i.User)
                .ThenInclude(c => c.ApplicationUser)
                .AsNoTracking()
                .AsQueryable();

            if (start.HasValue)
                query = query.Where(i => i.Date >= start.Value);
            if (end.HasValue)
                query = query.Where(i => i.Date <= end.Value);
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(i =>
                    (i.User.FullName != null && EF.Functions.Like(i.User.FullName, $"%{q}%")) ||
                    (i.User.UserId != null && EF.Functions.Like(i.User.UserId, $"%{q}%")) ||
                    (i.User.ApplicationUser.Email != null && EF.Functions.Like(i.User.ApplicationUser.Email, $"%{q}%"))
                );
            }

            var items = await query
                .OrderBy(i => i.Date).ThenBy(i => i.Id)
                .ToListAsync();

            var sb = new StringBuilder();
            sb.AppendLine("Date,UserId,FullName,Email,Amount,Source,Notes");
            foreach (var i in items)
            {
                var line = string.Join(",", new[]
                {
                    i.Date.ToString("yyyy-MM-dd"),
                    Csv(i.User?.UserId),
                    Csv(i.User?.FullName),
                    Csv(i.User?.ApplicationUser?.Email),
                    i.Amount.ToString(CultureInfo.InvariantCulture),
                    Csv(i.Source),
                    Csv(i.Notes)
                });
                sb.AppendLine(line);
            }

            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            var fileName = $"incomes_{DateTime.Now:yyyyMMddHHmmss}.csv";
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
