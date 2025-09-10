namespace GovFinance.Models.ViewModels
{
    public class HomeIndexVm
    {
        public bool IsUser { get; set; }
        public bool IsAdmin { get; set; }

        public decimal IncomeMonth { get; set; }
        public decimal ExpenseMonth { get; set; }
        public int ExpensePercent => IncomeMonth <= 0 ? 0 : (int)Math.Min(100, Math.Round((ExpenseMonth / IncomeMonth) * 100));

        public decimal BalanceMonth => IncomeMonth - ExpenseMonth;

        public decimal DeferredIncomeMonth { get; set; } // الدخل المؤجَّل هذا الشهر
        public decimal DeferredExpenseMonth { get; set; } // المصروف المؤجَّل هذا الشهر
        public List<LastEntryVm> LastEntries { get; set; } = new();

    }

    public class LastEntryVm
    {
        // "دخل" أو "مصروف"
        public string Type { get; set; } = default!;
        public string Source { get; set; } = default!;
        public decimal Amount { get; set; }
        public decimal PaidOrCollected { get; set; }   // Collected (دخل) أو Paid (مصروف)
        public decimal Outstanding { get; set; }       // المتبقي
        public DateOnly Date { get; set; }
        public string? Notes { get; set; }
    }
}
