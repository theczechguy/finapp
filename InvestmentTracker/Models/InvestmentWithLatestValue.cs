namespace InvestmentTracker.Models;

public class InvestmentWithLatestValue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public InvestmentCategory Category { get; set; }
    public InvestmentType Type { get; set; }
    public Currency Currency { get; set; }
    public string? Provider { get; set; }
    public decimal ChargeAmount { get; set; }
    public string? FamilyMemberName { get; set; }
    public InvestmentValue? LatestValue { get; set; }
    public decimal CurrentValue => LatestValue?.Value ?? 0;
}