using System;

namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class RevenueItemVM
    {
        public int BookingId { get; set; }

        public string CustomerName { get; set; } = "";

        public string CarPlate { get; set; } = "";

        public DateTime PaymentDate { get; set; }

        public decimal Amount { get; set; }
    }
}
