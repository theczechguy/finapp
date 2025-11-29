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
    public InvestmentCategory Category { get; set; }

    [Required]
    public Currency Currency { get; set; }

    public int? FamilyMemberId { get; set; }
    public FamilyMember? FamilyMember { get; set; }

    public DateTime? MaturityDate { get; set; }

    public decimal ChargeAmount { get; set; } = 0m;

    public ICollection<InvestmentValue> Values { get; set; } = new List<InvestmentValue>();

    public ICollection<ContributionSchedule> Schedules { get; set; } = new List<ContributionSchedule>();

    public ICollection<OneTimeContribution> OneTimeContributions { get; set; } = new List<OneTimeContribution>();
}
