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
    [Range(0.01, double.MaxValue)]
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
}
