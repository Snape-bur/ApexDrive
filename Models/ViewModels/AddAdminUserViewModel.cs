using System.ComponentModel.DataAnnotations;

namespace ApexDrive.Models.ViewModels
{
    public class AddAdminUserViewModel
    {
        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [Required, DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }
    }
}
