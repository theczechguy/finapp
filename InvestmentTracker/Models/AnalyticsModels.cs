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
}