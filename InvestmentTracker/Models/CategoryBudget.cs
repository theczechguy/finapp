using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class CategoryBudget
{
    public int Id { get; set; }

    [Required]
    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory? ExpenseCategory { get; set; }

    [Required]
    [Range(2020, 2100)]
    public int StartYear { get; set; }

    [Required]
    [Range(1, 12)]
    public int StartMonth { get; set; }

    [Range(2020, 2100)]
    public int? EndYear { get; set; }

    [Range(1, 12)]
    public int? EndMonth { get; set; }

    [Required]
    [Range(1, 10000000, ErrorMessage = "Budget amount must be between 1 and 10,000,000")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [NotMapped]
    public DateTime StartDate => new DateTime(StartYear, StartMonth, 1);

    [NotMapped]
    public DateTime? EndDate => EndYear.HasValue && EndMonth.HasValue
        ? new DateTime(EndYear.Value, EndMonth.Value, DateTime.DaysInMonth(EndYear.Value, EndMonth.Value))
        : null;

    public bool IsActiveForMonth(int year, int month)
    {
        var target = new DateTime(year, month, 1);
        var start = StartDate;
        var end = EndDate ?? DateTime.MaxValue;
        return start <= target && end >= target;
    }

    // Validation methods
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        // Validate date ranges
        if (EndYear.HasValue && EndMonth.HasValue)
        {
            var startDate = new DateTime(StartYear, StartMonth, 1);
            var endDate = new DateTime(EndYear.Value, EndMonth.Value, DateTime.DaysInMonth(EndYear.Value, EndMonth.Value));

            if (endDate < startDate)
            {
                yield return new ValidationResult("End date must be after start date", new[] { nameof(EndYear), nameof(EndMonth) });
            }
        }

        // Validate reasonable amount ranges
        if (Amount <= 0)
        {
            yield return new ValidationResult("Budget amount must be greater than zero", new[] { nameof(Amount) });
        }

        if (Amount > 10000000.00m)
        {
            yield return new ValidationResult("Budget amount cannot exceed 10,000,000", new[] { nameof(Amount) });
        }

        // Validate whole numbers only
        if (Amount % 1 != 0)
        {
            yield return new ValidationResult("Budget amount must be a whole number", new[] { nameof(Amount) });
        }
    }
}
