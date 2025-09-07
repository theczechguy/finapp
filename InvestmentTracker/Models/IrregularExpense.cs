using System;

namespace InvestmentTracker.Models;

public class IrregularExpense
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public DateTime Date { get; set; }
}
