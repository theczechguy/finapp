namespace InvestmentTracker.Models;

public class InvestmentSummary
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public InvestmentCategory Category { get; set; }
    public InvestmentType Type { get; set; }
    public Currency Currency { get; set; }
    public string? Provider { get; set; }
    public decimal ChargeAmount { get; set; }
}