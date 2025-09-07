using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class ExpenseSchedule
{
    public int Id { get; set; }

    [Required]
    public int RegularExpenseId { get; set; }
    public RegularExpense? RegularExpense { get; set; }

    // Month-based scheduling instead of specific dates
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

    [Required]
    public Frequency Frequency { get; set; } = Frequency.Monthly;

    // Computed properties for backward compatibility
    [NotMapped]
    public DateTime StartDate => new DateTime(StartYear, StartMonth, 1);

    [NotMapped]
    public DateTime? EndDate => EndYear.HasValue && EndMonth.HasValue
        ? new DateTime(EndYear.Value, EndMonth.Value, DateTime.DaysInMonth(EndYear.Value, EndMonth.Value))
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
}
