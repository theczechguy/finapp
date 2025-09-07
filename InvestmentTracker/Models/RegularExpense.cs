using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace InvestmentTracker.Models;

public class RegularExpense
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public Currency Currency { get; set; }

    public int? ExpenseCategoryId { get; set; }
    public ExpenseCategory? Category { get; set; }

    // Collection of schedules for temporal data
    public ICollection<ExpenseSchedule> Schedules { get; set; } = new List<ExpenseSchedule>();

    // Display amount for the current context (e.g., specific month)
    [NotMapped]
    public decimal? DisplayAmount { get; set; }

    // Computed properties for backward compatibility
    [NotMapped]
    public decimal Amount => DisplayAmount ?? GetCurrentAmount();

    [NotMapped]
    public Frequency Recurrence => GetCurrentFrequency();

    [NotMapped]
    public DateTime StartDate => GetStartDate();

    [NotMapped]
    public DateTime? EndDate => GetEndDate();

    public RegularExpense()
    {
        Currency = Currency.CZK;
    }

    private decimal GetCurrentAmount()
    {
        var currentSchedule = Schedules
            .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        return currentSchedule?.Amount ?? 0;
    }

    private Frequency GetCurrentFrequency()
    {
        var currentSchedule = Schedules
            .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefault();

        return currentSchedule?.Frequency ?? Frequency.Monthly;
    }

    private DateTime GetStartDate()
    {
        return Schedules.OrderBy(s => s.StartDate).FirstOrDefault()?.StartDate ?? DateTime.Today;
    }

    private DateTime? GetEndDate()
    {
        return Schedules.OrderByDescending(s => s.EndDate ?? DateTime.MaxValue).FirstOrDefault()?.EndDate;
    }
}
