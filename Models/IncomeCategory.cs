using System.ComponentModel.DataAnnotations;

namespace GovFinance.Models
{
    public class IncomeCategory
    {
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        [Range(0, 999999999999.99)]
        public decimal DefaultAmount { get; set; } = 0m;

        public bool IsActive { get; set; } = true;

        // تنقلات عكسية
        public ICollection<Income> Incomes { get; set; } = new List<Income>();
    }
}
