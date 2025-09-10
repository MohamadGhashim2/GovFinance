using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GovFinance.Models
{
    public class Income
    {
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CollectedAmount { get; set; }  // المبلغ المقبوض الآن

        [NotMapped]
        public decimal OutstandingAmount => Math.Max(0, Amount - CollectedAmount); // المتبقّي

        public string? Source { get; set; }   // يبقى اختياري

        public DateOnly Date { get; set; }
        public string? Notes { get; set; }

        public int? IncomeCategoryId { get; set; }
        public IncomeCategory? IncomeCategory { get; set; }

    }
}
