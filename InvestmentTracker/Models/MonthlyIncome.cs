using System;

namespace InvestmentTracker.Models;

public class MonthlyIncome
{
    public int Id { get; set; }
    public int IncomeSourceId { get; set; }
    public IncomeSource IncomeSource { get; set; } = null!;
    public DateTime Month { get; set; }
    public decimal ActualAmount { get; set; }
}
