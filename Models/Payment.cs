using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    public class Payment
    {
        [Key]
        public int PaymentId { get; set; }

        [Required]
        public int BookingId { get; set; }
        [ForeignKey(nameof(BookingId))]
        public Booking Booking { get; set; }

        [Column(TypeName = "decimal(10,2)")]
        public decimal Amount { get; set; }

        [Required, StringLength(30)]
        public string Method { get; set; } = "Cash";

        [Required, StringLength(30)]
        public string Status { get; set; } = "Pending";

        [StringLength(100)]
        public string? TransactionRef { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
