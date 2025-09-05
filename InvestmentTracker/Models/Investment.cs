using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace InvestmentTracker.Models;

public class Investment
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Provider { get; set; }

    [Required]
    public InvestmentType Type { get; set; }

    [Required]
    public Currency Currency { get; set; }

    [Range(0, double.MaxValue)]
    [Column(TypeName = "decimal(18,2)")]
    public decimal? RecurringAmount { get; set; }

    public ICollection<InvestmentValue> Values { get; set; } = new List<InvestmentValue>();
}
