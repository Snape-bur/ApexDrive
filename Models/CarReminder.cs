using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApexDrive.Models
{
    public class CarReminder
    {
        [Key] public int ReminderId { get; set; }

        [Required] public int CarId { get; set; }
        [ForeignKey(nameof(CarId))] public Car Car { get; set; }

        [ForeignKey(nameof(BranchId))] public Branch Branch { get; set; }
        public int? BranchId { get; set; }

        [Required, StringLength(50)] public string Type { get; set; } = string.Empty;
        [Required] public DateTime ReminderDate { get; set; }
        public bool IsCompleted { get; set; } = false;
        public DateTime? CompletedAt { get; set; }
        [StringLength(250)] public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

}
