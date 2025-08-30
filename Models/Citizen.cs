using System.ComponentModel.DataAnnotations;

namespace GovFinance.Models
{
    public class Citizen
    {
        public int Id { get; set; }

        [Required, StringLength(11)]        // مثال: رقم وطني (TR/…)
        public string NationalId { get; set; } = default!;

        [Required, StringLength(150)]
        public string FullName { get; set; } = default!;

        public DateOnly? BirthDate { get; set; }

        [StringLength(250)]
        public string? Address { get; set; }

        // ربط بحساب الهوية (مستخدم واحد لكل مواطن)
        [Required]
        public string ApplicationUserId { get; set; } = default!;
        public ApplicationUser ApplicationUser { get; set; } = default!;

        // تنقّلات
        public ICollection<Income> Incomes { get; set; } = new List<Income>();
        public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    }
}
