using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        // Relationships
        [Required]
        public string CustomerId { get; set; } = string.Empty;
        public AppUser Customer { get; set; }

        [Required]
        public int CarId { get; set; }
        public Car Car { get; set; }

        [Required]
        public int PickupBranchId { get; set; }
        public Branch PickupBranch { get; set; }

        [Required]
        public int DropoffBranchId { get; set; }
        public Branch DropoffBranch { get; set; }

        // Rental window
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }

        // Insurance & Accessories
        [StringLength(50)]
        public string InsuranceType { get; set; } = "Basic";
        public bool HasChildSeat { get; set; } = false;

        [Column(TypeName = "decimal(10,2)")]
        public decimal InsuranceCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ChildSeatCost { get; set; }

        // Pricing
        [Column(TypeName = "decimal(10,2)")]
        public decimal BaseCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal ExtrasCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; }

        // Status
        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        // Optional notes
        [StringLength(250)]
        public string? Notes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }


        // Payment (optional relationship)
        public Payment? Payment { get; set; }
    }
}
