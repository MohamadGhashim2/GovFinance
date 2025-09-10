using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GovFinance.Models
{
    public class IncomeCategory
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DefaultAmount { get; set; }

        [Required]
        public int UserId { get; set; }
        public User User { get; set; } = default!;

        // الربط الاختياري بمصروف ثابت
        public int? LinkedExpenseCategoryId { get; set; }
        public ExpenseCategory? LinkedExpenseCategory { get; set; }
    }
}
