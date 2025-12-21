using System.ComponentModel.DataAnnotations;

namespace ApexDrive.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100)]
        public string FullName { get; set; }

        [Phone(ErrorMessage = "Enter a valid phone number")]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }

        [EmailAddress]
        [Display(Name = "Email (read-only)")]
        public string Email { get; set; }  // just for display
    }
}
