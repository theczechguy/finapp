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
    public DateTime StartDate
    {
        get
        {
            // Validate input parameters before attempting DateTime construction
            if (StartYear < 1 || StartYear > 9999 || StartMonth < 1 || StartMonth > 12 || StartDay < 1 || StartDay > 31)
            {
                // Return a safe default date for invalid data
                return new DateTime(2020, 1, 1);
            }

            try
            {
                return new DateTime(StartYear, StartMonth, StartDay);
            }
            catch (ArgumentOutOfRangeException)
            {
                // If the stored StartDay is invalid for this month (e.g., Feb 30),
                // fall back to the last valid day of the month
                try
                {
                    int lastDayOfMonth = DateTime.DaysInMonth(StartYear, StartMonth);
                    int validDay = Math.Min(StartDay, lastDayOfMonth);
                    return new DateTime(StartYear, StartMonth, validDay);
                }
                catch
                {
                    // If even the fallback fails, return a safe default
                    return new DateTime(2020, 1, 1);
                }
            }
        }
    }

    [NotMapped]
    public DateTime? EndDate
    {
        get
        {
            if (!EndYear.HasValue || !EndMonth.HasValue || !EndDay.HasValue)
                return null;

            // Validate input parameters before attempting DateTime construction
            if (EndYear.Value < 1 || EndYear.Value > 9999 || EndMonth.Value < 1 || EndMonth.Value > 12 || EndDay.Value < 1 || EndDay.Value > 31)
            {
                // Return null for invalid end date data
                return null;
            }

            try
            {
                return new DateTime(EndYear.Value, EndMonth.Value, EndDay.Value);
            }
            catch (ArgumentOutOfRangeException)
            {
                // If the stored EndDay is invalid for this month, 
                // fall back to the last valid day of the month
                try
                {
                    int lastDayOfMonth = DateTime.DaysInMonth(EndYear.Value, EndMonth.Value);
                    int validDay = Math.Min(EndDay.Value, lastDayOfMonth);
                    return new DateTime(EndYear.Value, EndMonth.Value, validDay);
                }
                catch
                {
                    // If even the fallback fails, return null
                    return null;
                }
            }
        }
    }

    // Helper methods
    public bool IsActiveForMonth(int year, int month)
    {
        try
        {
            var scheduleStart = new DateTime(StartYear, StartMonth, 1);
            var scheduleEnd = EndDate ?? DateTime.MaxValue;
            var targetMonth = new DateTime(year, month, 1);

            return scheduleStart <= targetMonth && scheduleEnd >= targetMonth;
        }
        catch
        {
            // If date construction fails, assume the schedule is not active
            return false;
        }
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
        try
        {
            // Show quarterly expenses in months 1, 4, 7, 10 relative to start month
            var startDate = new DateTime(StartYear, StartMonth, 1);
            var targetDate = new DateTime(year, month, 1);
            var monthsDiff = (targetDate.Year - startDate.Year) * 12 + targetDate.Month - startDate.Month;
            return monthsDiff >= 0 && monthsDiff % 3 == 0;
        }
        catch
        {
            // If date construction fails, return false
            return false;
        }
    }

    private bool IsSemiAnnualMonth(int year, int month)
    {
        try
        {
            // Show semi-annual expenses every 6 months from start
            var startDate = new DateTime(StartYear, StartMonth, 1);
            var targetDate = new DateTime(year, month, 1);
            var monthsDiff = (targetDate.Year - startDate.Year) * 12 + targetDate.Month - startDate.Month;
            return monthsDiff >= 0 && monthsDiff % 6 == 0;
        }
        catch
        {
            // If date construction fails, return false
            return false;
        }
    }

    private bool IsAnnualMonth(int year, int month)
    {
        // Show annual expenses in the same month each year
        return month == StartMonth;
    }

    // Validation methods
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var results = new List<ValidationResult>();

        // Validate that StartDay is valid for the StartYear/StartMonth combination
        if (StartYear > 0 && StartMonth > 0 && StartDay > 0)
        {
            int maxDays = DateTime.DaysInMonth(StartYear, StartMonth);
            if (StartDay > maxDays)
            {
                results.Add(new ValidationResult(
                    $"Start day {StartDay} is not valid for {StartYear}-{StartMonth:D2}. " +
                    $"The maximum day for this month is {maxDays}.",
                    new[] { nameof(StartDay) }));
            }
        }

        // Validate EndDate if all components are provided
        if (EndYear.HasValue && EndMonth.HasValue && EndDay.HasValue)
        {
            if (EndYear.Value > 0 && EndMonth.Value > 0 && EndDay.Value > 0)
            {
                int maxDays = DateTime.DaysInMonth(EndYear.Value, EndMonth.Value);
                if (EndDay.Value > maxDays)
                {
                    results.Add(new ValidationResult(
                        $"End day {EndDay.Value} is not valid for {EndYear.Value}-{EndMonth.Value:D2}. " +
                        $"The maximum day for this month is {maxDays}.",
                        new[] { nameof(EndDay) }));
                }
            }

            // Validate that EndDate is after StartDate (only if both dates are valid)
            if (results.Count == 0 && StartDate > EndDate)
            {
                results.Add(new ValidationResult("End date must be after start date", new[] { nameof(EndYear), nameof(EndMonth), nameof(EndDay) }));
            }
        }

        return results;
    }
}
