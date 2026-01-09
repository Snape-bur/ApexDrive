using System.Collections.Generic;

namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class BranchPerformanceReportVM
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public List<BranchPerformanceItemVM> Branches { get; set; } = new();
    }
}