using System;
using System.ComponentModel.DataAnnotations;

namespace ApexDrive.Areas.Customer.Models
{
    public class BookingSearchViewModel
    {
        [Required(ErrorMessage = "Pickup branch is required")]
        public int PickupBranchId { get; set; }

        [Required(ErrorMessage = "Drop-off branch is required")]
        public int DropoffBranchId { get; set; }

        [Required(ErrorMessage = "Pickup date is required")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Return date is required")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public bool SameBranch { get; set; } = false;
    }
}
