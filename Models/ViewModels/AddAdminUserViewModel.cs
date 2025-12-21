using System.ComponentModel.DataAnnotations;

namespace ApexDrive.Models.ViewModels
{
    public class AddAdminUserViewModel
    {

        [Required, StringLength(100)]
        public string FullName { get; set; }

        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Branch is required")]
        public int BranchId { get; set; }
    }
}
