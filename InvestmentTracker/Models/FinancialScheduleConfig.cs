using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Models;

public class FinancialScheduleConfig
{
    [Key]
    public int Id { get; set; }

    // For multi-user, add UserId or similar
    public string? UserId { get; set; }

    [Required]
    public string ScheduleType { get; set; } = "Calendar"; // "Calendar" or "Custom"

    [Range(1, 31)]
    public int StartDay { get; set; } = 1;
}
