using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class ExpenseSchedule
{
    public int Id { get; set; }

    [Required]
    public int RegularExpenseId { get; set; }
    public RegularExpense? RegularExpense { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; }

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [Required]
    [Range(0.01, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Required]
    public Frequency Frequency { get; set; } = Frequency.Monthly;

    [Range(1, 31)]
    public int? DayOfMonth { get; set; }
}
