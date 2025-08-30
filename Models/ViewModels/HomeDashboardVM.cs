namespace GovFinance.Models.ViewModels
{
    public class HomeIndexVm
    {
        public bool IsCitizen { get; set; }
        public bool IsAdmin { get; set; }

        public decimal IncomeMonth { get; set; }
        public decimal ExpenseMonth { get; set; }
        public int ExpensePercent => IncomeMonth <= 0 ? 0 : (int)Math.Min(100, Math.Round((ExpenseMonth / IncomeMonth) * 100));

        public decimal BalanceMonth => IncomeMonth - ExpenseMonth;
    }
}
