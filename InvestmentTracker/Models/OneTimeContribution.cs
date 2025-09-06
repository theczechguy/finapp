using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Models;

public class OneTimeContribution
{
    public int Id { get; set; }
    public int InvestmentId { get; set; }
    public Investment? Investment { get; set; }

    [Required]
    public DateTime Date { get; set; }

    [Range(0.01, 100000000)]
    public decimal Amount { get; set; }
}
