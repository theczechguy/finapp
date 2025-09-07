namespace InvestmentTracker.Models;

public class IncomeSource
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
}
