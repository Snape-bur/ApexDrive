using ApexDrive.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ApexDrive.Data
{
    public class ApplicationDbContext : IdentityDbContext<AppUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // ✅ DbSets
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Car> Cars { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Payment> Payments { get; set; }
        public DbSet<CarReminder> CarReminders { get; set; }
        public DbSet<CarMaintenanceHistory> CarMaintenanceHistories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // 🔹 Relationships for multi-branch setup

            // Booking → PickupBranch (Restrict to avoid cascade delete)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.PickupBranch)
                .WithMany(br => br.PickupBookings)
                .HasForeignKey(b => b.PickupBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking → DropoffBranch (Restrict to avoid cascade delete)
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.DropoffBranch)
                .WithMany(br => br.DropoffBookings)
                .HasForeignKey(b => b.DropoffBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // AppUser → Branch (Admins only, nullable)
            modelBuilder.Entity<AppUser>()
                .HasOne(u => u.Branch)
                .WithMany(b => b.Admins)
                .HasForeignKey(u => u.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Car → Branch
            modelBuilder.Entity<Car>()
                .HasOne(c => c.Branch)
                .WithMany(b => b.Cars)
                .HasForeignKey(c => c.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // CarReminder → Branch
            modelBuilder.Entity<CarReminder>()
                .HasOne(r => r.Branch)
                .WithMany(b => b.Reminders)
                .HasForeignKey(r => r.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // CarMaintenanceHistory → Branch
            modelBuilder.Entity<CarMaintenanceHistory>()
                .HasOne(m => m.Branch)
                .WithMany(b => b.MaintenanceHistories)
                .HasForeignKey(m => m.BranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Payment → Booking (1:1)
            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Booking)
                .WithOne(b => b.Payment)
                .HasForeignKey<Payment>(p => p.BookingId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
