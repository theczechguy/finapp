using System.Collections.Generic;
using InvestmentTracker.Models;

namespace InvestmentTracker.ViewModels
{
    public class MonthlyExpenseViewModel
    {
        public DateTime SelectedDate { get; set; }
        public int Year => SelectedDate.Year;
        public int Month => SelectedDate.Month;
        public decimal TotalIncome { get; set; }
        public decimal TotalExpenses { get; set; }
        public decimal NetBalance => TotalIncome - TotalExpenses;

        public List<IncomeViewModel> Incomes { get; set; } = new();
        public List<OneTimeIncome> OneTimeIncomes { get; set; } = new();
        public List<RegularExpense> RegularExpenses { get; set; } = new();
        public List<IrregularExpense> IrregularExpenses { get; set; } = new();
        public Dictionary<string, decimal> ExpensesByCategory { get; set; } = new();

    public List<BudgetItemViewModel> Budgets { get; set; } = new();

    // Add schedule config for dashboard UI
    public FinancialScheduleConfig? ScheduleConfig { get; set; }
    
    public DateTime FinancialMonthStartDate { get; set; }
    public DateTime FinancialMonthEndDate { get; set; }
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

    public class BudgetHistoryItem
    {
        public int BudgetId { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int StartYear { get; set; }
        public int StartMonth { get; set; }
        public int? EndYear { get; set; }
        public int? EndMonth { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; }

        public string PeriodDisplay => EndDate.HasValue
            ? $"{StartDate.ToString("MMM yyyy")} - {EndDate.Value.ToString("MMM yyyy")}"
            : $"{StartDate.ToString("MMM yyyy")} - Ongoing";

        public string StatusDisplay => IsActive ? "Active" : "Inactive";
    }
}
