using System.Collections.Generic;
using InvestmentTracker.Models;

namespace InvestmentTracker.ViewModels
{
    public class MonthlyExpenseViewModel
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpenses;

        public List<IncomeViewModel> Incomes { get; set; } = new();
        public List<OneTimeIncome> OneTimeIncomes { get; set; } = new();
        public List<RegularExpense> RegularExpenses { get; set; } = new();
        public List<IrregularExpense> IrregularExpenses { get; set; } = new();
        public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new();

        public List<BudgetItemViewModel> Budgets { get; set; } = new();
    }

    public class BudgetItemViewModel
    {
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal? BudgetAmount { get; set; }
        public decimal SpentAmount { get; set; }
        public decimal Percent => BudgetAmount.HasValue && BudgetAmount > 0 ? (SpentAmount / BudgetAmount.Value) * 100 : 0;
        public string Status => !BudgetAmount.HasValue ? "No budget" : Percent >= 100 ? "Over" : Percent >= 80 ? "Near" : "Under";
    }
}
