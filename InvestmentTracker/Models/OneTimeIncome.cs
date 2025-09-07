using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class OneTimeIncome
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public Currency Currency { get; set; }
    public int? IncomeSourceId { get; set; }
    public IncomeSource? IncomeSource { get; set; }
}
