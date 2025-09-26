using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Models;

public class InvestmentProvider
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
}
