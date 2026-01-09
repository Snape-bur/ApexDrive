using System;
using System.Collections.Generic;

namespace ApexDrive.Areas.Admin.ViewModels.Reports
{
    public class VehicleUtilizationReportVM
    {
        public DateTime FromDate { get; set; }

        public DateTime ToDate { get; set; }

        public List<VehicleUtilizationItemVM> Vehicles { get; set; }
            = new List<VehicleUtilizationItemVM>();
    }
}
