using System.ComponentModel.DataAnnotations;

namespace ApexDrive.Models.ViewModels
{
    public class EditAdminUserViewModel
    {
        [Required]
        public string Id { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }


        [Required, EmailAddress]
        [Display(Name = "Email Address")]
        public string Email { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "New Password (optional)")]
        public string? NewPassword { get; set; }

        [Required]
        public int BranchId { get; set; }
    }
}
