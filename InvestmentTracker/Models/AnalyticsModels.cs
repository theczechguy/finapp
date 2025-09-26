namespace InvestmentTracker.Models
{
    public class CategoryExpenseData
    {
        public string Category { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Color { get; set; } = string.Empty;
    }

    public class MonthlyExpenseData
    {
        public string Month { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class CategoryComparisonData
    {
        public string Category { get; set; } = string.Empty;
        public decimal CurrentMonth { get; set; }
        public decimal PreviousMonth { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercent { get; set; }
        public string Trend { get; set; } = string.Empty; // "up", "down", "stable"
        public string Color { get; set; } = string.Empty;
    }

    public class MonthlyComparisonData
    {
        public string CurrentMonth { get; set; } = string.Empty;
        public string PreviousMonth { get; set; } = string.Empty;
        public decimal CurrentTotal { get; set; }
        public decimal PreviousTotal { get; set; }
        public decimal ChangeAmount { get; set; }
        public decimal ChangePercent { get; set; }
        public string Trend { get; set; } = string.Empty; // "up", "down", "stable"
        public List<CategoryComparisonData> CategoryComparisons { get; set; } = new();
    }

    public class IrregularExpenseCategoryAnalysis
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class RegularExpenseCategoryAnalysis
    {
        public string CategoryName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }

    public class MonthlyExpenseTrends
    {
        public string PeriodLabel { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal RegularExpenses { get; set; }
        public decimal IrregularExpenses { get; set; }
        public decimal TotalExpenses => RegularExpenses + IrregularExpenses;
    }

    public class InvestmentSeriesPoint
    {
        public DateTime AsOf { get; set; }
        public decimal Value { get; set; }
        public decimal Invested { get; set; }
    }
}