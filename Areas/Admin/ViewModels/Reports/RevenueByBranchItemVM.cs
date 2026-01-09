namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class RevenueByBranchItemVM
    {
        public int BranchId { get; set; }
        public string BranchName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalBookings { get; set; }
    }
}