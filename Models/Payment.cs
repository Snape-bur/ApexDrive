using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        // FK → Booking
        [Required]
        public int BookingId { get; set; }

        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }

        // Payment Details
        [Column(TypeName = "decimal(10,2)")]
        [Range(0, 1000000)]
        public decimal Amount { get; set; }

        [Required, StringLength(30)]
        public string Method { get; set; } = "Credit Card";

        [Required, StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string? TransactionRef { get; set; }

        // Track payment date/time
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    }
}
