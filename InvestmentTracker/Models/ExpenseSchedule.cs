using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class ExpenseSchedule
{
    public int Id { get; set; }

    [Required]
    public int RegularExpenseId { get; set; }
    public RegularExpense? RegularExpense { get; set; }

    // Month and day-based scheduling

    [Required]
    [Range(2020, 2100)]
    public int StartYear { get; set; }

    [Required]
    [Range(1, 12)]
    public int StartMonth { get; set; }

    [Required]
    [Range(1, 31)]
    public int StartDay { get; set; } = 1;


    [Range(2020, 2100)]
    public int? EndYear { get; set; }

    [Range(1, 12)]
    public int? EndMonth { get; set; }

    [Range(1, 31)]
    public int? EndDay { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public Frequency Frequency { get; set; } = Frequency.Monthly;

    // Computed properties for backward compatibility

    [NotMapped]
    public DateTime StartDate => new DateTime(StartYear, StartMonth, StartDay);

    [NotMapped]
    public DateTime? EndDate => EndYear.HasValue && EndMonth.HasValue && EndDay.HasValue
        ? new DateTime(EndYear.Value, EndMonth.Value, EndDay.Value)
        : null;

    // Helper methods
    public bool IsActiveForMonth(int year, int month)
    {
        var scheduleStart = new DateTime(StartYear, StartMonth, 1);
        var scheduleEnd = EndDate ?? DateTime.MaxValue;
        var targetMonth = new DateTime(year, month, 1);

        return scheduleStart <= targetMonth && scheduleEnd >= targetMonth;
    }

    public string GetMonthDisplay()
    {
        return $"{StartYear}-{StartMonth:D2}";
    }

    /// <summary>
    /// Determines if this expense should be applied in the given month based on frequency
    /// </summary>
    public bool ShouldApplyInMonth(int year, int month)
    {
        if (!IsActiveForMonth(year, month))
            return false;

        return Frequency switch
        {
            Frequency.Monthly => true,
            Frequency.Quarterly => IsQuarterlyMonth(year, month),
            Frequency.SemiAnnually => IsSemiAnnualMonth(year, month),
            Frequency.Annually => IsAnnualMonth(year, month),
            _ => false
        };
    }

    private bool IsQuarterlyMonth(int year, int month)
    {
        // Show quarterly expenses in months 1, 4, 7, 10 relative to start month
        var startDate = new DateTime(StartYear, StartMonth, 1);
        var targetDate = new DateTime(year, month, 1);
        var monthsDiff = (targetDate.Year - startDate.Year) * 12 + targetDate.Month - startDate.Month;
        return monthsDiff >= 0 && monthsDiff % 3 == 0;
    }

    private bool IsSemiAnnualMonth(int year, int month)
    {
        // Show semi-annual expenses every 6 months from start
        var startDate = new DateTime(StartYear, StartMonth, 1);
        var targetDate = new DateTime(year, month, 1);
        var monthsDiff = (targetDate.Year - startDate.Year) * 12 + targetDate.Month - startDate.Month;
        return monthsDiff >= 0 && monthsDiff % 6 == 0;
    }

    private bool IsAnnualMonth(int year, int month)
    {
        // Show annual expenses in the same month each year
        return month == StartMonth;
    }
}
