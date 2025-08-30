using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace GovFinance.Models
{
    public class Income
    {
        public int Id { get; set; }

        public int CitizenId { get; set; }

        [ValidateNever]
        public Citizen? Citizen { get; set; }  // <-- nullable + لا تتحقق منها

        [Required, Range(0, 999999999)]
        public decimal Amount { get; set; }

        [Required, StringLength(100)]
        public string Source { get; set; } = default!;

        public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [StringLength(300)]
        public string? Notes { get; set; }
    }
}
