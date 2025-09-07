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
        public List<RegularExpense> RegularExpenses { get; set; } = new();
        public List<IrregularExpense> IrregularExpenses { get; set; } = new();
        public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new();
    }
}
