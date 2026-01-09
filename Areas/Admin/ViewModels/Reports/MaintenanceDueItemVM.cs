namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class MaintenanceDueItemVM
    {
        public int ReminderId { get; set; }
        public string CarPlate { get; set; }
        public string BranchName { get; set; }
        public string MaintenanceType { get; set; }
        public DateTime DueDate { get; set; }
        public bool IsOverdue { get; set; }
    }
}
