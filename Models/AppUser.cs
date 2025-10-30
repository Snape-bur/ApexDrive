using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace ApexDrive.Models
{
    public class AppUser : IdentityUser
    {
        [Required, StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Address { get; set; }

        // nullable for SuperAdmin & Customer
        public int? BranchId { get; set; }

        [ForeignKey(nameof(BranchId))]
        public Branch? Branch { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
