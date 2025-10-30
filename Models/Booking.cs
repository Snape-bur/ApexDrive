using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Active,
        Completed,
        Cancelled
    }

    public class Booking
    {
        [Key]
        public int BookingId { get; set; }

        [Required]
        public string CustomerId { get; set; }
        [ForeignKey(nameof(CustomerId))]
        public AppUser Customer { get; set; }

        [Required]
        public int CarId { get; set; }
        [ForeignKey(nameof(CarId))]
        public Car Car { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public BookingStatus Status { get; set; } = BookingStatus.Pending;

        [Column(TypeName = "decimal(10,2)")]
        public decimal BaseCost { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal TotalCost { get; set; }

        // Multi-branch support
        [Required]
        public int PickupBranchId { get; set; }
        [ForeignKey(nameof(PickupBranchId))]
        public Branch PickupBranch { get; set; }

        [Required]
        public int DropoffBranchId { get; set; }
        [ForeignKey(nameof(DropoffBranchId))]
        public Branch DropoffBranch { get; set; }

        // One-to-one Payment (defined on Payment side)
        public Payment? Payment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
