using InvestmentTracker.Models;

namespace InvestmentTracker.ViewModels
{
    public class IncomeViewModel
    {
        public int IncomeSourceId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal ExpectedAmount { get; set; }
        public decimal ActualAmount { get; set; }
        public Currency Currency { get; set; }
    }
}
