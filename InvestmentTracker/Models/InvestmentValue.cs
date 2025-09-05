using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class InvestmentValue
{
    public int Id { get; set; }

    [Required]
    public int InvestmentId { get; set; }

    public Investment? Investment { get; set; }

    [Required]
    public DateTime AsOf { get; set; }

    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Value { get; set; }
}
