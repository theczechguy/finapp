using System.ComponentModel.DataAnnotations;

namespace InvestmentTracker.Models
{
    public class UserAccount
    {
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string AccountNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? BankName { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
