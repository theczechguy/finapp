using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public enum ContributionFrequency
{
    Monthly = 1
}

public class ContributionSchedule
{
    public int Id { get; set; }

    [Required]
    public int InvestmentId { get; set; }
    public Investment? Investment { get; set; }

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
    public ContributionFrequency Frequency { get; set; } = ContributionFrequency.Monthly;

    [Range(1, 31)]
    public int? DayOfMonth { get; set; }
}
