using System;
using System.Collections.Generic;

namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class MaintenanceDueReportVM
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public int TotalDue { get; set; }
        public int OverdueCount { get; set; }

        public List<MaintenanceDueItemVM> Items { get; set; }
    }
}