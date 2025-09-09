using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;

namespace InvestmentTracker.Models;

public class RegularExpense
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public Currency Currency { get; set; }

    public int? ExpenseCategoryId { get; set; }
    public ExpenseCategory? Category { get; set; }

    // Family/Individual categorization
    public ExpenseType ExpenseType { get; set; } = ExpenseType.Family;
    public int? FamilyMemberId { get; set; }
    public FamilyMember? FamilyMember { get; set; }

    // Collection of schedules for temporal data
    public ICollection<ExpenseSchedule> Schedules { get; set; } = new List<ExpenseSchedule>();

    // Display amount for the current context (e.g., specific month)
    [NotMapped]
    public decimal? DisplayAmount { get; set; }

    // Computed properties for backward compatibility
    [NotMapped]
    public decimal Amount => DisplayAmount ?? GetCurrentAmount();

    // Context-aware display frequency (set by service for selected month)
    [NotMapped]
    public Frequency? DisplayFrequency { get; set; }

    [NotMapped]
    public Frequency Recurrence => DisplayFrequency ?? GetCurrentFrequency();

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

    // Get frequency info for display purposes
    [NotMapped]
    public string FrequencyDisplay
    {
        get
        {
            var frequency = Recurrence;
            return frequency switch
            {
                Frequency.Monthly => "Monthly",
                Frequency.Quarterly => "Quarterly",
                Frequency.SemiAnnually => "Semi-Annual",
                Frequency.Annually => "Annual",
                _ => "Monthly"
            };
        }
    }

    [NotMapped]
    public bool IsAlternativeSchedule => Recurrence != Frequency.Monthly;

    [NotMapped] 
    public string FrequencyBadgeClass
    {
        get
        {
            return Recurrence switch
            {
                Frequency.Monthly => "bg-primary",
                Frequency.Quarterly => "bg-info",
                Frequency.SemiAnnually => "bg-warning text-dark",
                Frequency.Annually => "bg-success",
                _ => "bg-primary"
            };
        }
    }
}