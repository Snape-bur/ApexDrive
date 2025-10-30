using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Models
{
    [Index(nameof(BranchName), IsUnique = false)]
    public class Branch
    {
        [Key]
        public int BranchId { get; set; }

        [Required, StringLength(100)]
        public string BranchName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Address { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        // optional manager (FK → AppUser)
        public string? ManagerId { get; set; }

        [ForeignKey(nameof(ManagerId))]
        public AppUser? Manager { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // navigation collections
        public ICollection<AppUser>? Admins { get; set; }
        public ICollection<Car>? Cars { get; set; }
        public ICollection<Booking>? PickupBookings { get; set; }
        public ICollection<Booking>? DropoffBookings { get; set; }
        public ICollection<CarMaintenanceHistory>? MaintenanceHistories { get; set; }
        public ICollection<CarReminder>? Reminders { get; set; }
    }
}
