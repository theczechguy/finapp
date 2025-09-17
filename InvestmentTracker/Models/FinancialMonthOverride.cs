using System;
using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Models;

public class FinancialMonthOverride
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The first day of the month this override applies to (e.g., 2025-09-01 for September 2025).
    /// </summary>
    [Required]
    public DateTime TargetMonth { get; set; }

    /// <summary>
    /// The actual, non-standard start date for this financial month.
    /// </summary>
    [Required]
    public DateTime OverrideStartDate { get; set; }
    
    // For multi-user, add UserId or similar
    public string? UserId { get; set; }
}
