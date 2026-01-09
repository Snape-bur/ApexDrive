using System;
using System.Collections.Generic;
using ApexDrive.Models; // make sure this namespace matches where CarReminder exists

namespace ApexDrive.Models.ViewModels
{
    public class ReminderSummary
    {
        public int OverdueCount { get; set; }
        public int DueTodayCount { get; set; }
        public int UpcomingCount { get; set; }
        public List<CarReminder>? UpcomingReminders { get; set; }
    }

    public class DashboardViewModel
    {
        // 🏢 Branch Info
        public string BranchName { get; set; } = "All Branches";

        // 📅 Date Filter (NEW)
        public DateTime? SelectedDate { get; set; }

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        // 📊 Summary Stats
        public int TotalCars { get; set; }
        public int ActiveBookings { get; set; }
        public int UpcomingReminders { get; set; }
        public int CompletedMaintenances { get; set; }

        // 💰 Financial Summary
        public decimal TotalRevenue { get; set; }

        // 🏢 Super Admin Only
        public List<RevenuePerBranchVM> RevenuePerBranch { get; set; } = new();

        // 🧰 Maintenance Records
        public List<CarMaintenanceHistory> RecentMaintenanceRecords { get; set; } = new();

        // 🔔 Reminder Banner Summary
        public ReminderSummary ReminderSummary { get; set; } = new();
    }


    public class RevenuePerBranchVM
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; } = "";
        public decimal Revenue { get; set; }
    }

}
