using System;
using System.Collections.Generic;

namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class RevenueByBranchReportVM
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public decimal GrandTotalRevenue { get; set; }

        public List<RevenueByBranchItemVM> Branches { get; set; } = new();
    }
}