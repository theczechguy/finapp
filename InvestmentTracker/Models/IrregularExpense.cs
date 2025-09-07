using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class IrregularExpense
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    
    [Column(TypeName = "decimal(18, 2)")]
    public decimal Amount { get; set; }
    public Currency Currency { get; set; }

    public int? ExpenseCategoryId { get; set; }
    public ExpenseCategory? Category { get; set; }
    public DateTime Date { get; set; }

    public IrregularExpense()
    {
        Currency = Currency.CZK;
    }
}
