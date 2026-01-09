using System;

namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class VehicleUtilizationItemVM
    {
        public int CarId { get; set; }

        public string PlateNumber { get; set; }

        public string BranchName { get; set; }

        public int TotalBookings { get; set; }

        public int TotalRentedDays { get; set; }

        public decimal UtilizationRate { get; set; } // %
    }
}
