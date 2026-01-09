namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class RevenueReportVM
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }

        public decimal TotalRevenue { get; set; }
        public List<RevenueItemVM> Items { get; set; }
    }

}
