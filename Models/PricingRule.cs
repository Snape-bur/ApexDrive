using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    public class PricingRule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // e.g., "Summer Peak", "Christmas"

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        [Column(TypeName = "decimal(5,2)")]
        public decimal Multiplier { get; set; } // e.g., 1.5 for 50% increase, 0.8 for 20% discount

        public bool IsRecurring { get; set; } // If true, ignore the year (applies every year)
    }
}
