using System.Collections.Generic;

namespace ApexDrive.Models.ViewModels
{
    public class DashboardViewModel
    {
        // 🏢 Branch Info
        public string BranchName { get; set; } = "All Branches";

        // 📊 Summary Stats
        public int TotalCars { get; set; }
        public int ActiveBookings { get; set; }
        public int UpcomingReminders { get; set; }
        public int CompletedMaintenances { get; set; }

        // 💰 Financial Summary
        public decimal TotalRevenue { get; set; }

        // 🧰 Maintenance Records (for recent 5 items)
        public List<CarMaintenanceHistory> RecentMaintenanceRecords { get; set; } = new();
    }
}
