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
        // Order by raw date components to avoid triggering StartDate property evaluation
        var currentSchedule = Schedules
            .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
            .OrderByDescending(s => s.StartYear)
            .ThenByDescending(s => s.StartMonth)
            .ThenByDescending(s => s.StartDay)
            .FirstOrDefault();

        return currentSchedule?.Amount ?? 0;
    }

    private Frequency GetCurrentFrequency()
    {
        // Order by raw date components to avoid triggering StartDate property evaluation
        var currentSchedule = Schedules
            .Where(s => s.StartDate <= DateTime.Today && (s.EndDate == null || s.EndDate >= DateTime.Today))
            .OrderByDescending(s => s.StartYear)
            .ThenByDescending(s => s.StartMonth)
            .ThenByDescending(s => s.StartDay)
            .FirstOrDefault();

        return currentSchedule?.Frequency ?? Frequency.Monthly;
    }

    private DateTime GetStartDate()
    {
        // Order by raw date components to avoid triggering StartDate property evaluation
        // which could fail on invalid dates in the database
        var earliestSchedule = Schedules
            .OrderBy(s => s.StartYear)
            .ThenBy(s => s.StartMonth)
            .ThenBy(s => s.StartDay)
            .FirstOrDefault();

        return earliestSchedule?.StartDate ?? DateTime.Today;
    }

    private DateTime? GetEndDate()
    {
        // Order by raw date components to avoid triggering EndDate property evaluation
        var latestSchedule = Schedules
            .Where(s => s.EndYear.HasValue && s.EndMonth.HasValue && s.EndDay.HasValue)
            .OrderByDescending(s => s.EndYear)
            .ThenByDescending(s => s.EndMonth)
            .ThenByDescending(s => s.EndDay)
            .FirstOrDefault();

        return latestSchedule?.EndDate;
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