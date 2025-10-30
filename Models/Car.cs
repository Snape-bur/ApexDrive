using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    public class Car
    {
        [Key]
        public int CarId { get; set; }

        [Required, StringLength(20)]
        public string PlateNumber { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Brand { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string Model { get; set; } = string.Empty;

        [Column(TypeName = "decimal(10,2)")]
        [Range(1, 1000000, ErrorMessage = "Daily rate must be greater than zero.")]
        public decimal DailyRate { get; set; }

        [Range(0, 1000000, ErrorMessage = "Mileage must be a valid number.")]
        public int Mileage { get; set; }

        public bool IsAvailable { get; set; } = true;

        public DateTime? InsuranceExpiry { get; set; }

        public DateTime? LastServiceDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // FK → Branch
        [Required(ErrorMessage = "Branch selection is required.")]
        [ForeignKey(nameof(BranchId))]
        public int BranchId { get; set; }

        public Branch? Branch { get; set; }

        // navigation
        public ICollection<Booking>? Bookings { get; set; }
        public ICollection<CarReminder>? Reminders { get; set; }
        public ICollection<CarMaintenanceHistory>? MaintenanceHistories { get; set; }
    }
}
