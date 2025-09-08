using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Models;

public class FamilyMember
{
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(50)]
    public string Relationship { get; set; } = string.Empty; // e.g., "Spouse", "Child", "Self", etc.

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
