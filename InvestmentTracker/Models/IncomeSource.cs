using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class IncomeSource
{
    public IncomeSource()
    {
        Currency = Currency.CZK;
    }

    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18, 2)")]
    public decimal ExpectedAmount { get; set; }

    public Currency Currency { get; set; }
}
