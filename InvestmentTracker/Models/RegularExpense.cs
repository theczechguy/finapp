using System;

namespace InvestmentTracker.Models;

public class RegularExpense
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory Category { get; set; } = null!;
    public Frequency Recurrence { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
