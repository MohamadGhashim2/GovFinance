using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation; // <-- أضِف هذا

namespace GovFinance.Models
{
    public class Expense
    {
        public int Id { get; set; }

        // لا حاجة لـ [Required] هنا؛ العمود أصلاً NOT NULL
        public int CitizenId { get; set; }

        [ValidateNever]              // لا تتحقق منها في الـ ModelState
        public Citizen? Citizen { get; set; }  // <-- صارت nullable

        [Required, Range(0, 999999999)]
        public decimal Amount { get; set; }

        [Required, StringLength(100)]
        public string Category { get; set; } = default!;

        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}
