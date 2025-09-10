namespace GovFinance.Models.ViewModels
{
    public class AdminUserLedgerVm
    {
        // User info
        public int Id { get; set; }
        public string? UserId { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }

        // Data
        public IEnumerable<Income> Incomes { get; set; } = [];
        public IEnumerable<Expense> Expenses { get; set; } = [];

        // Totals
        public decimal TotalIncome { get; set; }
        public decimal TotalExpense { get; set; }
        public decimal Net => TotalIncome - TotalExpense;

        // For UI (filters)
        public string? Start { get; set; }
        public string? End { get; set; }
    }
}
