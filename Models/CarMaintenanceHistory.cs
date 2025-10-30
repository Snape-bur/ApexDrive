using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;  // ← add this

    public class CarMaintenanceHistory
    {
        [Key]
        public int HistoryId { get; set; }

        [Required]
        public int CarId { get; set; }

        [ForeignKey(nameof(CarId))]
        [ValidateNever]           // ← do not validate nav prop
        public Car? Car { get; set; }   // ← nullable

        public int BranchId { get; set; }   // ← no [Required]

        [ForeignKey(nameof(BranchId))]
        [ValidateNever]           // ← do not validate nav prop
        public Branch? Branch { get; set; } // ← nullable

        [Required]
        public DateTime ServiceDate { get; set; }

        public int Mileage { get; set; }

        [StringLength(250)]
        public string? Notes { get; set; }

        [StringLength(100)]
        public string? ServiceType { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
